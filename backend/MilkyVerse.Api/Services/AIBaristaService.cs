using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services;

public interface IAIBaristaService
{
    Task<ApiResponse<AIChatResponse>> ChatAsync(int? userId, AIChatRequest request);
}

public class AIBaristaService : IAIBaristaService
{
    private readonly MilkyVerseDbContext _context;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public AIBaristaService(MilkyVerseDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _config = config;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<ApiResponse<AIChatResponse>> ChatAsync(int? userId, AIChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return ApiResponse<AIChatResponse>.Fail("Nội dung tin nhắn không được để trống.");
        }

        // Get or create session
        ChatSession? session = null;
        if (request.SessionId.HasValue && request.SessionId.Value > 0)
        {
            session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId.Value);
        }

        if (session == null)
        {
            session = new ChatSession
            {
                UserId = userId,
                SessionTitle = request.Message.Length > 30 ? request.Message[..27] + "..." : request.Message,
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        // Save User Message
        _context.ChatMessages.Add(new ChatMessage
        {
            SessionId = session.Id,
            SenderRole = "User",
            Content = request.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Load active menu products & toppings for AI context
        var activeProducts = await _context.Products.Where(p => p.IsActive).ToListAsync();
        var activeToppings = await _context.Toppings.Where(t => t.IsAvailable).ToListAsync();

        var geminiApiKey = _config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        AIChatResponse response;

        if (!string.IsNullOrWhiteSpace(geminiApiKey) && geminiApiKey != "YOUR_GEMINI_API_KEY_HERE")
        {
            try
            {
                response = await CallGeminiApiAsync(session.Id, request.Message, activeProducts, geminiApiKey);
            }
            catch
            {
                // Graceful fallback to localized barista engine when remote AI fails
                response = FallbackLocalBarista(session.Id, request.Message, activeProducts, activeToppings, isErrorFallback: true);
            }
        }
        else
        {
            response = FallbackLocalBarista(session.Id, request.Message, activeProducts, activeToppings, isErrorFallback: false);
        }

        // Save Assistant Message
        _context.ChatMessages.Add(new ChatMessage
        {
            SessionId = session.Id,
            SenderRole = "Assistant",
            Content = response.Reply,
            SuggestedDrinkJson = JsonSerializer.Serialize(response.SuggestedDrinks),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return ApiResponse<AIChatResponse>.Ok(response);
    }

    private async Task<AIChatResponse> CallGeminiApiAsync(int sessionId, string userPrompt, List<Product> products, string apiKey)
    {
        var menuSummary = string.Join("\n", products.Select(p => $"- ID: {p.Id}, Tên: '{p.Name}', Giá: {p.BasePrice:N0}đ, Mô tả: {p.Description}"));

        var systemPrompt = $@"Bạn là AI Barista ngân hà của thương hiệu trà sữa MilkyVerse. Hãy tư vấn đồ uống một cách thân thiện, nhiệt tình, tinh tế bằng tiếng Việt.
Dưới đây là thực đơn hiện có:
{menuSummary}

QUY TẮC BẮT BUỘC:
1. Bạn chỉ được gợi ý các món có trong danh sách trên bằng đúng ID và Tên.
2. Không bịa đặt giá cả hoặc món không tồn tại.
3. Trả về câu trả lời tự nhiên kèm danh sách gợi ý.";

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = $"{systemPrompt}\n\nKhách hàng hỏi: \"{userPrompt}\"" }
                    }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var httpRes = await _httpClient.PostAsync(endpoint, content);
        httpRes.EnsureSuccessStatusCode();

        var json = await httpRes.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var replyText = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "Chào bạn! Mình có thể giúp gì cho bạn hôm nay?";

        // Verify and link products from actual menu
        var suggestions = new List<SuggestedDrinkDto>();
        foreach (var p in products)
        {
            if (replyText.Contains(p.Name, StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    EstimatedPrice = p.BasePrice,
                    SugarRecommend = "50%",
                    IceRecommend = "70%",
                    RecommendedToppings = new List<string> { "Trân Châu Hoàng Kim Dẻo Dai" },
                    Reason = "Được AI Barista đề xuất theo sở thích của bạn."
                });
            }
        }

        if (!suggestions.Any())
        {
            var bestSeller = products.FirstOrDefault(p => p.IsBestSeller) ?? products.FirstOrDefault();
            if (bestSeller != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = bestSeller.Id,
                    Name = bestSeller.Name,
                    Description = bestSeller.Description,
                    EstimatedPrice = bestSeller.BasePrice,
                    SugarRecommend = "50%",
                    IceRecommend = "70%",
                    RecommendedToppings = new List<string> { "Trân Châu Hoàng Kim Dẻo Dai" },
                    Reason = "Món bán chạy nhất ngân hà MilkyVerse được yêu thích nhất."
                });
            }
        }

        return new AIChatResponse
        {
            SessionId = sessionId,
            Reply = replyText,
            SuggestedDrinks = suggestions,
            IsAiModelLive = true
        };
    }

    private static AIChatResponse FallbackLocalBarista(int sessionId, string message, List<Product> products, List<Topping> toppings, bool isErrorFallback)
    {
        var lower = message.ToLower();
        var suggestions = new List<SuggestedDrinkDto>();
        string reply;

        string notice = isErrorFallback
            ? "[Lưu ý: Dịch vụ Cloud AI tạm thời gián đoạn, AI Barista chuyển sang Chế độ Mô phỏng Thông minh trực tiếp từ MilkyVerse]\n\n"
            : "[Chế độ: AI Barista Mô phỏng Nội bộ - Đang tư vấn dựa trên danh mục thực tế MilkyVerse]\n\n";

        if (lower.Contains("ít ngọt") || lower.Contains("healthy") || lower.Contains("giảm cân") || lower.Contains("không ngọt") || lower.Contains("eat clean"))
        {
            var oat = products.FirstOrDefault(p => p.Slug.Contains("yen-mach") || p.Slug.Contains("oat")) 
                   ?? products.FirstOrDefault(p => p.Slug.Contains("oolong"));
            var tea = products.FirstOrDefault(p => p.Slug.Contains("matcha") || p.Slug.Contains("tra-xanh"));

            reply = notice + "Chào bạn yêu thích lối sống lành mạnh! Với gu thanh mát ít calo, mình đề xuất bạn nên chọn mức 30% đường và ưu tiên sữa yến mạch (Oat Milk) hoặc trà xanh thanh nhiệt:";

            if (oat != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = oat.Id,
                    Name = oat.Name,
                    Description = oat.Description,
                    EstimatedPrice = oat.BasePrice,
                    SugarRecommend = "30%",
                    IceRecommend = "70%",
                    RecommendedToppings = new List<string> { "Trân Châu Trắng 3Q Giòn Sần Sật" },
                    Reason = "Sữa yến mạch Oatly Thụy Điển thuần chay, béo thanh tự nhiên và không gây tăng cân."
                });
            }

            if (tea != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = tea.Id,
                    Name = tea.Name,
                    Description = tea.Description,
                    EstimatedPrice = tea.BasePrice,
                    SugarRecommend = "30%",
                    IceRecommend = "50%",
                    RecommendedToppings = new List<string> { "Trân Châu Trắng 3Q Giòn Sần Sật" },
                    Reason = "Giàu chất chống oxy hóa tự nhiên, vị trà thanh dịu rất dễ chịu."
                });
            }
        }
        else if (lower.Contains("béo") || lower.Contains("kem") || lower.Contains("cheese") || lower.Contains("phô mai") || lower.Contains("đậm"))
        {
            var cheese = products.FirstOrDefault(p => p.Slug.Contains("cheese") || p.Slug.Contains("macchiato")) 
                      ?? products.FirstOrDefault(p => p.IsBestSeller);

            reply = notice + "Dành cho tín đồ mê đắm vị béo bồng bềnh! Lớp kem Cheese Foam béo mặn sánh mịn độc quyền của MilkyVerse kết hợp cùng hồng trà đậm đà chắc chắn sẽ làm bạn xiêu lòng:";

            if (cheese != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = cheese.Id,
                    Name = cheese.Name,
                    Description = cheese.Description,
                    EstimatedPrice = cheese.BasePrice,
                    SugarRecommend = "50%",
                    IceRecommend = "70%",
                    RecommendedToppings = new List<string> { "Kem Phô Mai Cheese Foam Béo Mặn", "Pudding Trứng Nao Nao Mềm Mịn" },
                    Reason = "Lớp foam phô mai Anchor béo ngậy cân bằng hoàn hảo với vị chát nhẹ của hồng trà."
                });
            }
        }
        else if (lower.Contains("chua") || lower.Contains("trái cây") || lower.Contains("nắng") || lower.Contains("thanh nhiệt") || lower.Contains("giải khát"))
        {
            var fruit = products.FirstOrDefault(p => p.Slug.Contains("xoai") || p.Slug.Contains("trai-cay"))
                     ?? products.FirstOrDefault(p => p.Slug.Contains("lai"));

            reply = notice + "Để đánh bay cảm giác oi bức, một ly trà hoa quả nhiệt đới tươi mát chua ngọt hài hòa sẽ là lựa chọn chuẩn xác nhất:";

            if (fruit != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = fruit.Id,
                    Name = fruit.Name,
                    Description = fruit.Description,
                    EstimatedPrice = fruit.BasePrice,
                    SugarRecommend = "50%",
                    IceRecommend = "100%",
                    RecommendedToppings = new List<string> { "Trân Châu Trắng 3Q Giòn Sần Sật" },
                    Reason = "Vị xoài chín mọng thơm lừng hòa quyện cùng cốt lục trà lài thanh thoát."
                });
            }
        }
        else
        {
            var sig = products.FirstOrDefault(p => p.Slug.Contains("signature")) ?? products.FirstOrDefault();
            var oolong = products.FirstOrDefault(p => p.Slug.Contains("o-long"));

            reply = notice + "MilkyVerse xin chào bạn! Nếu bạn đang phân vân chưa biết chọn món gì, đây là 2 'ngôi sao' được các fan trà sữa yêu thích nhất tháng này:";

            if (sig != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = sig.Id,
                    Name = sig.Name,
                    Description = sig.Description,
                    EstimatedPrice = sig.BasePrice,
                    SugarRecommend = "70%",
                    IceRecommend = "70%",
                    RecommendedToppings = new List<string> { "Trân Châu Hoàng Kim Dẻo Dai" },
                    Reason = "Công thức kinh điển ủ lạnh 12h với hạt trân châu nấu đường đen dẻo thơm ngất ngây."
                });
            }

            if (oolong != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = oolong.Id,
                    Name = oolong.Name,
                    Description = oolong.Description,
                    EstimatedPrice = oolong.BasePrice,
                    SugarRecommend = "50%",
                    IceRecommend = "50%",
                    RecommendedToppings = new List<string> { "Pudding Trứng Nao Nao Mềm Mịn" },
                    Reason = "Vị trà ô long nướng khói than hồng quyến rũ, đậm đà mà không gắt."
                });
            }
        }

        return new AIChatResponse
        {
            SessionId = sessionId,
            Reply = reply,
            SuggestedDrinks = suggestions,
            IsAiModelLive = false
        };
    }
}
