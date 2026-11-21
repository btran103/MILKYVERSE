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
    Task<ApiResponse<AIStatusResponse>> GetStatusAsync();
    Task<ApiResponse<AIStatusResponse>> ConfigureApiKeyAsync(AIConfigRequest request);
}

public class AIBaristaService : IAIBaristaService
{
    private readonly MilkyVerseDbContext _context;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    private static string? _runtimeApiKey;
    private static string _runtimeProvider = "Google Gemini API";
    private static string _runtimeModel = "gemini-2.5-flash";

    public AIBaristaService(MilkyVerseDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _config = config;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(25);
    }

    private string? ResolveApiKey(string? requestKey = null)
    {
        if (!string.IsNullOrWhiteSpace(requestKey)) return requestKey.Trim();
        if (!string.IsNullOrWhiteSpace(_runtimeApiKey)) return _runtimeApiKey.Trim();
        var cfgKey = _config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(cfgKey) && cfgKey != "YOUR_GEMINI_API_KEY_HERE") return cfgKey.Trim();
        return null;
    }

    private static string DetectProvider(string? apiKey, string? preferredProvider = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredProvider)) return preferredProvider;
        if (string.IsNullOrWhiteSpace(apiKey)) return "MilkyVerse Dataset API";
        if (apiKey.StartsWith("gsk_", StringComparison.OrdinalIgnoreCase)) return "Groq Cloud LLM API";
        if (apiKey.StartsWith("sk-or-", StringComparison.OrdinalIgnoreCase)) return "OpenRouter LLM API";
        if (apiKey.StartsWith("sk-", StringComparison.OrdinalIgnoreCase)) return "OpenAI ChatGPT API";
        return "Google Gemini API";
    }

    private static string MaskKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        if (key.Length <= 8) return "****";
        return $"{key[..4]}...{key[^4..]}";
    }

    public async Task<ApiResponse<AIStatusResponse>> GetStatusAsync()
    {
        var activeKey = ResolveApiKey();
        var provider = DetectProvider(activeKey, _runtimeProvider);
        var menuCount = await _context.Products.CountAsync(p => p.IsActive);
        var toppingCount = await _context.Toppings.CountAsync(t => t.IsAvailable);

        return ApiResponse<AIStatusResponse>.Ok(new AIStatusResponse
        {
            IsConfigured = !string.IsNullOrWhiteSpace(activeKey),
            Provider = !string.IsNullOrWhiteSpace(activeKey) ? provider : "MilkyVerse Smart Dataset Engine (REST API /api/ai/chat)",
            ActiveModel = !string.IsNullOrWhiteSpace(activeKey) ? _runtimeModel : "milkyverse-barista-v2",
            MaskedApiKey = MaskKey(activeKey),
            EndpointUrl = "/api/ai/chat",
            ActiveMenuCount = menuCount,
            ActiveToppingCount = toppingCount
        });
    }

    public async Task<ApiResponse<AIStatusResponse>> ConfigureApiKeyAsync(AIConfigRequest request)
    {
        var cleanKey = request.ApiKey?.Trim() ?? string.Empty;
        _runtimeApiKey = string.IsNullOrWhiteSpace(cleanKey) ? null : cleanKey;

        if (!string.IsNullOrWhiteSpace(_runtimeApiKey))
        {
            _runtimeProvider = DetectProvider(_runtimeApiKey, request.Provider);
            if (!string.IsNullOrWhiteSpace(request.Model))
            {
                _runtimeModel = request.Model.Trim();
            }
            else if (_runtimeProvider.Contains("Groq"))
            {
                _runtimeModel = "llama-3.3-70b-versatile";
            }
            else if (_runtimeProvider.Contains("OpenAI"))
            {
                _runtimeModel = "gpt-4o-mini";
            }
            else if (_runtimeProvider.Contains("OpenRouter"))
            {
                _runtimeModel = "google/gemini-2.5-flash";
            }
            else
            {
                _runtimeModel = "gemini-2.5-flash";
            }
        }
        else
        {
            _runtimeProvider = "MilkyVerse Smart Dataset Engine";
            _runtimeModel = "milkyverse-barista-v2";
        }

        // Persist to appsettings.Local.json (gitignored) so API key is never leaked to Git
        try
        {
            var localSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");
            Dictionary<string, object> dict = new();
            if (File.Exists(localSettingsPath))
            {
                var raw = await File.ReadAllTextAsync(localSettingsPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, object>>(raw) ?? new();
            }
            dict["Gemini"] = new { ApiKey = _runtimeApiKey ?? "" };
            var updated = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(localSettingsPath, updated);
        }
        catch
        {
            // Ignore file write errors in read-only environments; runtime memory remains active
        }

        return await GetStatusAsync();
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

        // Load recent orders if user is authenticated
        List<Order> userRecentOrders = new();
        if (userId.HasValue && userId.Value > 0)
        {
            userRecentOrders = await _context.Orders
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.CreatedAt)
                .Take(3)
                .Include(o => o.OrderDetails)
                .ToListAsync();
        }

        var activeApiKey = ResolveApiKey(request.ApiKey);
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            _runtimeApiKey = request.ApiKey.Trim();
            _runtimeProvider = DetectProvider(_runtimeApiKey, request.Provider);
        }

        AIChatResponse response;

        if (!string.IsNullOrWhiteSpace(activeApiKey))
        {
            try
            {
                response = await CallExternalLlmApiAsync(session, request.Message, activeProducts, activeToppings, userRecentOrders, activeApiKey, request.Provider);
            }
            catch (Exception ex)
            {
                // Graceful fallback to localized barista engine when remote AI fails
                response = FallbackLocalBarista(session.Id, request.Message, activeProducts, activeToppings, userRecentOrders, isErrorFallback: true, apiError: ex.Message);
            }
        }
        else
        {
            response = FallbackLocalBarista(session.Id, request.Message, activeProducts, activeToppings, userRecentOrders, isErrorFallback: false);
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

    private async Task<AIChatResponse> CallExternalLlmApiAsync(ChatSession session, string userPrompt, List<Product> products, List<Topping> toppings, List<Order> userOrders, string apiKey, string? preferredProvider = null)
    {
        // Build rich menu categorized dataset summary with dietary tags across ALL 213 products
        var drinkList = products.Where(p => (p.ProductType ?? "DRINK") == "DRINK").Take(140).Select(p =>
        {
            var isOat = p.Slug.Contains("yen-mach") || p.Slug.Contains("oat");
            var isJuice = p.Slug.Contains("nuoc-ep") || p.Slug.Contains("soda");
            var isNoCaffeine = isJuice || p.Slug.Contains("sua-tuoi");
            var tagStr = isNoCaffeine ? "[Không Caffeine]" : isOat ? "[Thuần Chay/Sữa Yến Mạch]" : "[Có Trà/Caffeine]";
            return $"- [Đồ Uống] ID: {p.Id}, Tên: '{p.Name}', Slug: '{p.Slug}', Giá: {p.BasePrice:N0}đ {tagStr}, Mô tả: {p.Description}";
        });

        var foodList = products.Where(p => (p.ProductType ?? "DRINK") == "FOOD").Take(60).Select(p =>
            $"- [Ăn Vặt] ID: {p.Id}, Tên: '{p.Name}', Slug: '{p.Slug}', Giá: {p.BasePrice:N0}đ, Độ cay: Cấp {p.SpicyLevel ?? 0}, Mô tả: {p.Description}"
        );

        var comboList = products.Where(p => (p.ProductType ?? "DRINK") == "COMBO").Take(25).Select(p =>
            $"- [Combo Tiết Kiệm] ID: {p.Id}, Tên: '{p.Name}', Slug: '{p.Slug}', Giá: {p.BasePrice:N0}đ, Mô tả: {p.Description}"
        );

        var menuSummary = string.Join("\n", drinkList.Concat(foodList).Concat(comboList));
        var toppingSummary = string.Join(", ", toppings.Take(20).Select(t => $"{t.Name} (+{t.Price:N0}đ)"));
        var userOrdersSummary = userOrders.Any()
            ? string.Join("; ", userOrders.Select(o => $"Đơn {o.OrderCode ?? ("#MV-" + o.Id)}: trạng thái '{o.Status}', thanh toán '{o.PaymentStatus}', tổng {o.TotalAmount:N0}đ"))
            : "Khách chưa có đơn hàng nào gần đây.";

        var systemPrompt = $@"Bạn là Milky - AI Barista thông minh, chu đáo và tinh tế của thương hiệu trà sữa & ăn vặt MilkyVerse.
Bạn tư vấn bằng tiếng Việt thân thiện, ngọt ngào, chuẩn phong cách MilkyVerse.

THỰC ĐƠN MILKYVERSE HIỆN CÓ (Dữ liệu thật từ Database):
{menuSummary}

DANH SÁCH TOPPING KHẢ DỤNG:
{toppingSummary}

THÔNG TIN ĐƠN HÀNG GẦN ĐÂY CỦA KHÁCH:
{userOrdersSummary}

QUY TẮC TƯ VẤN BẮT BUỘC:
1. Khi khách đang bị ho, đau họng, rát cổ, khàn tiếng, cảm lạnh hoặc trời lạnh:
   - Khuyên khách tuyệt đối KHÔNG uống đá lạnh (chọn **0% đá / uống ấm nóng**) và tránh đồ chiên rán nhiều dầu mỡ hoặc đồ cay.
   - Chỉ đề xuất các món dịu cổ họng, kháng khuẩn, giàu Vitamin C và ấm áp như: **Trà Đào Cam Sả** (có sả & cam ấm họng), **Trà Tắc Xí Muội / Mật Ong** (tắc và xí muội trị ho, dịu cổ), **Trà Hoa Cúc Mật Ong / Hồng Trà Mật Ong**, **Nước Ép Cam Tươi** (tăng đề kháng), **Sữa Tươi Yến Mạch / Sữa Tươi Ấm**.
2. Khi khách buồn ngủ, mệt mỏi, cần tỉnh táo chạy deadline, học bài, làm việc:
   - Đề xuất các món giàu năng lượng & caffeine tự nhiên như: **Cà Phê Muối Huế**, **Bạc Xỉu Đá**, **Matcha Latte Uji Kyoto**, **Hồng Trà Sữa Hoàng Gia**, **Trà Ô Long Kem Cheese**.
3. Khi khách hỏi món uống buổi tối / sợ mất ngủ / không caffeine: Chỉ gợi ý các món Nước Ép tươi, Soda, hoặc Sữa Tươi Trân Châu Đường Đen (hoàn toàn không chứa caffeine). Tuyệt đối KHÔNG gợi ý trà xanh/hồng trà/ô long/cà phê.
4. Khi khách ăn kiêng, giảm cân, ít ngọt, eat clean: Gợi ý chọn mức 30% đường hoặc 0% đường, đề xuất Trà Oolong mộc hoặc Sữa yến mạch Oatly (thuần chay).
5. Khi khách tìm món mát lạnh dưới 40.000đ: Đề xuất Trà Đào Cam Sả (39.000đ), Trà Tắc Xí Muội (32.000đ) hoặc Nước Ép Dưa Hấu (38.000đ).
6. Khi khách hỏi món quán không bán (như pizza, burger, lẩu...): Lịch sự từ chối quán không có pizza/burger và giới thiệu các món ăn vặt signature cực cuốn: Bánh tráng trộn tôm thịt, Cá viên chiên ngũ sắc, Chân gà sốt Thái, Mì cay 7 cấp độ.
7. Khi khách muốn tự thiết kế hoặc tự pha: Giới thiệu phòng lab Tự Pha Chế tại đường dẫn [/diy](/diy) để tự chọn cốt trà, loại sữa, và topping.
8. Khi khách hỏi về đơn hàng / hủy đơn / tiến độ giao: Dựa vào thông tin đơn hàng ở trên để giải đáp cụ thể và hướng dẫn khách kiểm tra chi tiết tại đường dẫn [/orders](/orders).
9. Luôn dùng đúng tên món trong thực đơn ở trên và đề xuất từ 4-5 món phù hợp kèm theo lời khuyên về % đường, % đá và topping phù hợp nhất.";

        var provider = DetectProvider(apiKey, preferredProvider ?? _runtimeProvider);
        string replyText = string.Empty;
        string usedModel = _runtimeModel;

        // Build multi-turn history from session
        var recentHistory = session.Messages
            .OrderByDescending(m => m.CreatedAt)
            .Take(6)
            .OrderBy(m => m.CreatedAt)
            .ToList();

        if (provider.Contains("Groq") || provider.Contains("OpenAI") || provider.Contains("OpenRouter"))
        {
            var endpoint = provider.Contains("Groq")
                ? "https://api.groq.com/openai/v1/chat/completions"
                : provider.Contains("OpenRouter")
                    ? "https://openrouter.ai/api/v1/chat/completions"
                    : "https://api.openai.com/v1/chat/completions";

            usedModel = provider.Contains("Groq")
                ? "llama-3.3-70b-versatile"
                : provider.Contains("OpenRouter")
                    ? "google/gemini-2.5-flash"
                    : "gpt-4o-mini";

            var messagesList = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            foreach (var h in recentHistory)
            {
                messagesList.Add(new
                {
                    role = string.Equals(h.SenderRole, "User", StringComparison.OrdinalIgnoreCase) ? "user" : "assistant",
                    content = h.Content
                });
            }
            messagesList.Add(new { role = "user", content = userPrompt });

            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, endpoint);
            reqMsg.Headers.Add("Authorization", $"Bearer {apiKey}");
            reqMsg.Content = new StringContent(JsonSerializer.Serialize(new
            {
                model = usedModel,
                messages = messagesList,
                temperature = 0.7
            }), Encoding.UTF8, "application/json");

            var httpRes = await _httpClient.SendAsync(reqMsg);
            if (!httpRes.IsSuccessStatusCode)
            {
                var errBody = await httpRes.Content.ReadAsStringAsync();
                throw new Exception($"{provider} trả về mã lỗi {(int)httpRes.StatusCode}: {errBody}");
            }

            var json = await httpRes.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            replyText = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Chào bạn! Mình có thể giúp gì cho bạn hôm nay?";
        }
        else
        {
            // Google Gemini API: Try gemini-2.5-flash -> fallback to gemini-2.0-flash -> gemini-1.5-flash-latest
            var candidateModels = new[] { "gemini-2.5-flash", "gemini-2.0-flash", "gemini-2.5-flash-lite" };
            Exception? lastEx = null;

            foreach (var modelName in candidateModels)
            {
                try
                {
                    var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
                    var historyText = recentHistory.Any()
                        ? "\nLỊCH SỬ TRÒ CHUYỆN GẦN ĐÂY:\n" + string.Join("\n", recentHistory.Select(h => $"{h.SenderRole}: {h.Content}"))
                        : "";

                    var payload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[]
                                {
                                    new { text = $"{systemPrompt}{historyText}\n\nKhách hàng hỏi: \"{userPrompt}\"" }
                                }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = 0.7,
                            maxOutputTokens = 1024
                        }
                    };

                    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    var httpRes = await _httpClient.PostAsync(endpoint, content);
                    if (!httpRes.IsSuccessStatusCode)
                    {
                        var errBody = await httpRes.Content.ReadAsStringAsync();
                        throw new Exception($"Gemini ({modelName}) HTTP {(int)httpRes.StatusCode}: {errBody}");
                    }

                    var json = await httpRes.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    replyText = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "Chào bạn! Mình có thể giúp gì cho bạn hôm nay?";

                    usedModel = modelName;
                    _runtimeModel = modelName;
                    lastEx = null;
                    break;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                }
            }

            if (lastEx != null && string.IsNullOrWhiteSpace(replyText))
            {
                throw lastEx;
            }
        }

        // Verify and link products from actual menu mentioned in reply (up to 6 diverse dishes)
        var suggestions = new List<SuggestedDrinkDto>();
        foreach (var p in products)
        {
            if (replyText.Contains(p.Name, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(p.Slug) && replyText.Contains(p.Slug, StringComparison.OrdinalIgnoreCase)))
            {
                suggestions.Add(BuildSuggestedDto(p, $"Được AI Barista ({usedModel}) đề xuất theo khẩu vị của bạn."));
                if (suggestions.Count >= 6) break;
            }
        }

        if (suggestions.Count < 4)
        {
            var extraBestSellers = products
                .Where(p => p.IsBestSeller && suggestions.All(s => s.ProductId != p.Id))
                .Take(4 - suggestions.Count);

            foreach (var best in extraBestSellers)
            {
                suggestions.Add(BuildSuggestedDto(best, "Món Best-Seller bán chạy nhất ngân hà MilkyVerse."));
            }
        }

        return new AIChatResponse
        {
            SessionId = session.Id,
            Reply = replyText,
            SuggestedDrinks = suggestions,
            IsAiModelLive = true,
            ModelName = $"{provider} ({usedModel})"
        };
    }

    private static SuggestedDrinkDto BuildSuggestedDto(Product p, string reason, string? sugarOverride = null, string? iceOverride = null, List<string>? toppingsOverride = null)
    {
        var isFood = (p.ProductType ?? "DRINK") == "FOOD";
        var isCombo = (p.ProductType ?? "DRINK") == "COMBO";
        return new SuggestedDrinkDto
        {
            ProductId = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            ImageUrl = p.ImageUrl,
            Description = p.Description,
            EstimatedPrice = p.BasePrice,
            SugarRecommend = sugarOverride ?? (isFood ? "Không áp dụng" : isCombo ? "Tùy chọn 50%" : "50%"),
            IceRecommend = iceOverride ?? (isFood ? "Không áp dụng" : isCombo ? "Tùy chọn 70%" : "70%"),
            RecommendedToppings = toppingsOverride ?? (isFood
                ? new List<string> { !string.IsNullOrWhiteSpace(p.DefaultSauce) ? p.DefaultSauce : "Tương ớt cay nồng" }
                : new List<string> { "Trân Châu Hoàng Kim Nấu Mật", "Kem Phô Mai Macchiato" }),
            Reason = reason
        };
    }

    private static AIChatResponse FallbackLocalBarista(int sessionId, string message, List<Product> products, List<Topping> toppings, List<Order> userOrders, bool isErrorFallback, string? apiError = null)
    {
        var lower = message.ToLower().Trim();
        var suggestions = new List<SuggestedDrinkDto>();
        string reply;

        var words = System.Text.RegularExpressions.Regex
            .Split(lower, @"[^a-zA-Z0-9àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]+")
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToHashSet();

        bool HasWord(params string[] targets) => targets.Any(t => words.Contains(t));
        bool HasPhrase(params string[] phrases) => phrases.Any(ph => lower.Contains(ph));

        var matchedProducts = products
            .Where(p => p.Name.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(w => w.Length > 3 && words.Contains(w))
                     || lower.Contains(p.Name.ToLower())
                     || (!string.IsNullOrEmpty(p.Slug) && lower.Contains(p.Slug.Replace("-", " "))))
            .Take(6)
            .ToList();

        // Kịch bản 0A: Bị ho / Đau họng / Rát cổ / Cảm lạnh / Khàn tiếng / Viêm họng / Trời lạnh
        if (HasWord("ho", "cảm", "ốm", "sốt", "lạnh") || HasPhrase("đau họng", "rát họng", "rát cổ", "viêm họng", "khàn tiếng", "dịu họng", "ấm bụng", "ấm cổ", "trị ho", "mật ong"))
        {
            var soothingList = products
                .Where(p => p.Slug.Contains("tac") || p.Slug.Contains("xi-muoi") || p.Slug.Contains("mat-ong") || p.Slug.Contains("cam-sa") || p.Slug.Contains("cam") || p.Slug.Contains("hoa-cuc") || p.Slug.Contains("gung") || p.Slug.Contains("yen-mach"))
                .Where(p => (p.ProductType ?? "DRINK") == "DRINK")
                .Take(5)
                .ToList();

            if (soothingList.Count < 4)
            {
                var backupWarm = products
                    .Where(p => (p.ProductType ?? "DRINK") == "DRINK" && (p.Slug.Contains("dao") || p.Slug.Contains("sua-tuoi") || p.Slug.Contains("oolong")) && soothingList.All(s => s.Id != p.Id))
                    .Take(5 - soothingList.Count);
                soothingList.AddRange(backupWarm);
            }

            foreach (var item in soothingList)
            {
                suggestions.Add(BuildSuggestedDto(
                    item,
                    "Giàu Vitamin C / thảo mộc tự nhiên, pha chế 0% đá (ấm) giúp làm dịu cơn ho và ấm cổ họng.",
                    sugarOverride: "50% (Dịu nhẹ)",
                    iceOverride: "0% Đá (Uống Ấm)",
                    toppingsOverride: new List<string> { "Thạch Nha Đam Mát Dịu", "Không dùng topping giòn lạnh" }
                ));
            }

            reply = "Dạ bạn đang bị **ho / rát cổ họng** thì tuyệt đối không nên uống đá lạnh hay ăn đồ chiên rán, cay nóng nha! 🍯\n\n"
                  + "Milky đã tự động chỉnh công thức **0% Đá (Pha ấm nóng)** và chọn ra **Top 5 thức uống làm dịu cổ họng, tiêu đờm & tăng sức đề kháng** tốt nhất cho bạn:\n\n"
                  + string.Join("\n", suggestions.Select(s => $"• 🍵 **{s.Name}** ({s.EstimatedPrice:N0}đ) — *[Công thức: 0% Đá / Uống ấm]*: {s.Description}"))
                  + "\n\n💡 *Lời khuyên từ Milky:* Bạn nên chọn **Trà Tắc Xí Muội / Mật Ong**, **Trà Đào Cam Sả** hoặc **Nước Cam Ấm** để tinh dầu sả, tắc và vitamin C làm dịu họng nhanh nhất nhé!";
        }
        // Kịch bản 0B: Buồn ngủ / Mệt mỏi / Chạy deadline / Cần tỉnh táo / Học bài / Làm việc
        else if (HasPhrase("buồn ngủ", "tỉnh táo", "tỉnh ngủ", "mệt mỏi", "deadline", "học bài", "làm việc", "tập trung", "năng lượng", "cà phê", "cafe"))
        {
            var energyList = products
                .Where(p => p.Slug.Contains("ca-phe") || p.Slug.Contains("cafe") || p.Slug.Contains("bac-xiu") || p.Slug.Contains("matcha") || p.Slug.Contains("hong-tra") || p.Slug.Contains("oolong"))
                .Where(p => (p.ProductType ?? "DRINK") == "DRINK")
                .Take(5)
                .ToList();

            foreach (var item in energyList)
            {
                suggestions.Add(BuildSuggestedDto(
                    item,
                    "Hàm lượng Caffeine & L-Theanine tự nhiên giúp tỉnh táo tức thì, tập trung cao độ.",
                    sugarOverride: "50%",
                    iceOverride: "70%",
                    toppingsOverride: new List<string> { "Trân Châu Hoàng Kim Nấu Mật", "Kem Phô Mai Macchiato" }
                ));
            }

            reply = "Bạn đang cần nạp năng lượng và đánh bay cơn buồn ngủ để tập trung đúng không nè? ⚡\n\n"
                  + "Milky gợi ý ngay **Top 5 thức uống giàu Caffeine & L-Theanine tự nhiên** giúp bạn tỉnh táo sảng khoái tức thì:\n\n"
                  + string.Join("\n", suggestions.Select(s => $"• ☕ **{s.Name}** ({s.EstimatedPrice:N0}đ): {s.Description}"))
                  + "\n\nBạn chốt ly nào để Milky pha ngay cho kịp giờ chạy deadline nhé!";
        }
        // Kịch bản 0C: Tâm trạng buồn / Stress / Áp lực / Thất tình / Cần chữa lành (Mood booster)
        else if (HasWord("buồn", "stress", "chán") || HasPhrase("áp lực", "tâm trạng", "thất tình", "chữa lành", "vui vẻ", "ngọt ngào"))
        {
            var moodList = products
                .Where(p => p.Slug.Contains("socola") || p.Slug.Contains("chocolate") || p.Slug.Contains("duong-den") || p.Slug.Contains("matcha") || p.Slug.Contains("dau") || p.Slug.Contains("cheese"))
                .Take(5)
                .ToList();

            foreach (var item in moodList)
            {
                suggestions.Add(BuildSuggestedDto(
                    item,
                    "Vị ngọt ngào béo mịn kích thích hormone hạnh phúc Endorphin & Serotonin, xua tan căng thẳng.",
                    sugarOverride: "70%",
                    iceOverride: "70%"
                ));
            }

            reply = "Ôm bạn một cái nha! Những lúc căng thẳng hay tụt mood thì một ly đồ uống ngọt ngào béo mịn sẽ giúp cơ thể tiết ra hormone hạnh phúc để chữa lành tâm trạng ngay lập tức 💖:\n\n"
                  + string.Join("\n", suggestions.Select(s => $"• 🍫 **{s.Name}** ({s.EstimatedPrice:N0}đ): {s.Description}"))
                  + "\n\nHãy tự thưởng cho bản thân một ly thật ngon hôm nay nhé!";
        }
        // Kịch bản 0D: Thích ăn cay / Mì cay / Đồ cay nóng
        else if (HasWord("cay") || HasPhrase("ăn cay", "mì cay", "chân gà", "sốt thái", "xé lưỡi"))
        {
            var spicyList = products
                .Where(p => (p.ProductType ?? "DRINK") == "FOOD" && ((p.SpicyLevel ?? 0) > 0 || p.Slug.Contains("cay") || p.Slug.Contains("thai") || p.Slug.Contains("banh-trang") || p.Slug.Contains("tokbokki")))
                .Take(5)
                .ToList();

            foreach (var item in spicyList)
            {
                suggestions.Add(BuildSuggestedDto(item, $"Món ăn vặt cay đậm đà (Cấp độ cay tùy chọn 1-7) cực kỳ kích thích vị giác."));
            }

            reply = "Team mê ăn cay chuẩn bị xuýt xoa với **Top 5 món ăn vặt cay nồng đậm vị** đỉnh nhất MilkyVerse đây ạ 🌶️🔥:\n\n"
                  + string.Join("\n", suggestions.Select(s => $"• 🌶️ **{s.Name}** ({s.EstimatedPrice:N0}đ): {s.Description}"))
                  + "\n\nĐừng quên gọi kèm một ly trà trái cây mát lạnh để vừa ăn cay vừa giải nhiệt nha!";
        }
        // Kịch bản 1: Buổi tối / Không caffeine / Sợ mất ngủ / Trẻ em
        else if (lower.Contains("tối") || lower.Contains("mất ngủ") || lower.Contains("không caffeine") || lower.Contains("caffeine") || lower.Contains("không trà") || lower.Contains("trẻ em") || lower.Contains("bà bầu"))
        {
            var noCaffeineList = products
                .Where(p => p.Slug.Contains("dua-hau") || p.Slug.Contains("nuoc-ep") || p.Slug.Contains("soda") || (p.Slug.Contains("sua-tuoi") && p.Slug.Contains("duong-den")) || p.Slug.Contains("sua-chua"))
                .Take(5)
                .ToList();

            foreach (var item in noCaffeineList)
            {
                var isMilk = item.Slug.Contains("sua-tuoi");
                suggestions.Add(BuildSuggestedDto(
                    item,
                    isMilk ? "100% sữa tươi thanh trùng không cốt trà, thơm béo ấm bụng." : "Trái cây tươi / Soda 0% caffeine, thanh mát dễ ngủ.",
                    sugarOverride: isMilk ? "50%" : "30%",
                    iceOverride: "50%"
                ));
            }

            reply = "Dạ uống buổi tối không lo mất ngủ thì Milky tuyển chọn ngay cho bạn **Top 5 món 0% Caffeine** thanh mát, dễ ngủ nhất trong thực đơn MilkyVerse:\n\n"
                  + string.Join("\n", suggestions.Select(s => $"• 🌙 **{s.Name}** ({s.EstimatedPrice:N0}đ): {s.Description}"))
                  + "\n\nBạn ưng ly nào bấm **Thêm giỏ** ngay bên dưới để Milky chuẩn bị nhé!";
        }
        // Kịch bản 2: Healthy / Ăn kiêng / Ít ngọt / Giảm cân / Eat clean / Không đường
        else if (lower.Contains("ít ngọt") || lower.Contains("healthy") || lower.Contains("giảm cân") || lower.Contains("không đường") || lower.Contains("eat clean") || lower.Contains("ăn kiêng"))
        {
            var healthyList = products
                .Where(p => p.Slug.Contains("yen-mach") || p.Slug.Contains("oat") || p.Slug.Contains("oolong") || p.Slug.Contains("tra-xanh") || p.Slug.Contains("hat-chia") || p.Slug.Contains("nuoc-ep"))
                .Take(5)
                .ToList();

            foreach (var item in healthyList)
            {
                var isOat = item.Slug.Contains("yen-mach") || item.Slug.Contains("oat");
                suggestions.Add(BuildSuggestedDto(
                    item,
                    isOat ? "Sữa yến mạch thuần chay giàu chất xơ, không cholesterol." : "Thanh lọc cơ thể, ít calo, hỗ trợ giữ dáng.",
                    sugarOverride: isOat ? "30%" : "0%",
                    iceOverride: "50%"
                ));
            }

            reply = "Chào bạn yêu thích lối sống Healthy! Milky gợi ý ngay **Top 5 thức uống Eat-Clean & Ít Calo** (0% - 30% đường) được yêu thích nhất:\n\n"
                  + string.Join("\n", suggestions.Select(s => $"• 🥗 **{s.Name}** ({s.EstimatedPrice:N0}đ): {s.Description}"))
                  + "\n\nTất cả các món đều đã được Milky chỉnh sẵn công thức ít đường tốt cho vóc dáng!";
        }
        // Kịch bản 3: Món rẻ / Dưới 40k / Giá sinh viên / Tiết kiệm
        else if (lower.Contains("dưới 40") || lower.Contains("40k") || lower.Contains("rẻ") || lower.Contains("sinh viên") || lower.Contains("tiết kiệm"))
        {
            var budgetList = products
                .Where(p => p.BasePrice <= 40000)
                .OrderByDescending(p => p.IsBestSeller)
                .Take(6)
                .ToList();

            foreach (var item in budgetList)
            {
                suggestions.Add(BuildSuggestedDto(item, $"Giá hạt dẻ chỉ {item.BasePrice:N0}đ mà ngon chuẩn vị MilkyVerse."));
            }

            reply = $"Dạ có ngay **Top {suggestions.Count} món ngon mát lạnh giá hạt dẻ dưới 40.000đ** cực hot tại MilkyVerse nè:\n\n"
                  + string.Join("\n", suggestions.Select(s => $"• 🍑 **{s.Name}** — chỉ **{s.EstimatedPrice:N0}đ**: {s.Description}"))
                  + "\n\nBạn chọn món nào để Milky lên đơn liền tay nhé?";
        }
        // Kịch bản 4: Đồ ăn vặt / Đói bụng / Hỏi món quán không bán (pizza, burger, ăn kèm...)
        else if (lower.Contains("pizza") || lower.Contains("burger") || lower.Contains("ăn vặt") || lower.Contains("đói") || lower.Contains("món ăn") || lower.Contains("ăn kèm") || lower.Contains("cá viên") || lower.Contains("bánh tráng") || lower.Contains("mì cay") || lower.Contains("chân gà"))
        {
            var snacks = products
                .Where(p => (p.ProductType ?? "DRINK") == "FOOD")
                .OrderByDescending(p => p.IsBestSeller)
                .Take(6)
                .ToList();

            foreach (var item in snacks)
            {
                suggestions.Add(BuildSuggestedDto(item, "Đồ ăn vặt nóng giòn chuẩn vị ăn kèm trà sữa cực cuốn."));
            }

            reply = (lower.Contains("pizza") || lower.Contains("burger")
                ? "Dạ MilkyVerse không phục vụ pizza hay burger ạ! Nhưng quán có nguyên thiên đường **đồ ăn vặt đường phố nóng hổi** cực kỳ đưa miệng để nhâm nhi cùng trà sữa:\n\n"
                : "Uống trà sữa mà có thêm mâm ăn vặt giòn rụm nóng hổi thì đúng chuẩn bài luôn! Milky lên ngay **Top 6 món ăn vặt đắt khách nhất**:\n\n")
                  + string.Join("\n", suggestions.Select(s => $"• 🍟 **{s.Name}** ({s.EstimatedPrice:N0}đ): {s.Description}"))
                  + "\n\nBạn muốn gắp những món nào vào giỏ hàng hôm nay nè?";
        }
        // Kịch bản 5: Tự pha chế / DIY
        else if (lower.Contains("tự pha") || lower.Contains("diy") || lower.Contains("tự mix") || lower.Contains("công thức"))
        {
            var baseTeas = products
                .Where(p => p.Slug.Contains("oolong") || p.Slug.Contains("matcha") || p.Slug.Contains("hong-tra") || p.Slug.Contains("yen-mach"))
                .Take(4)
                .ToList();

            suggestions.Add(new SuggestedDrinkDto
            {
                ProductId = baseTeas.FirstOrDefault()?.Id ?? 1,
                Name = "Phòng Lab Trà Sữa Tự Phối (DIY Drink Studio)",
                Slug = "diy",
                Description = "Tự chọn Cốt Trà + Sữa Tươi/Yến Mạch + Độ Ngọt + 15 loại Topping theo gu riêng tại /diy",
                EstimatedPrice = 38000,
                SugarRecommend = "Tùy chọn 0% - 100%",
                IceRecommend = "Tùy chọn 0% - 100%",
                RecommendedToppings = new List<string> { "Trân Châu Hoàng Kim", "Kem Phô Mai Macchiato", "Thạch Dừa Giòn" },
                Reason = "Bấm 'Tự Pha' để mở ngay phòng lab DIY Studio."
            });

            foreach (var item in baseTeas)
            {
                suggestions.Add(BuildSuggestedDto(item, "Công thức nền tảng gợi ý để bạn tham khảo khi phối trà."));
            }

            reply = "Bạn hoàn toàn có thể tự tay sáng tạo ly trà sữa độc quyền tại phòng lab **DIY Drink Studio** ([Mở Phòng Lab Tự Pha Chế](/diy)) của MilkyVerse nha!\n\n"
                  + "Tại trang [/diy](/diy), bạn được tự do chọn:\n"
                  + "1. **Cốt trà**: Hồng trà Ceylon, Lục trà lài, Ô long khói, Matcha Uji Kyoto.\n"
                  + "2. **Loại sữa**: Sữa tươi Dalat, Sữa đặc béo, Sữa yến mạch Oatly hoặc Sữa hạnh nhân.\n"
                  + "3. **Lượng đường đá & 15 loại Topping** tùy thích!\n\n"
                  + "Dưới đây là link vào thẳng DIY Studio kèm 4 công thức cốt trà hot nhất để bạn tham khảo:";
        }
        // Kịch bản 6: Tra cứu đơn hàng / Vận chuyển
        else if (lower.Contains("đơn") || lower.Contains("giao hàng") || lower.Contains("shipper") || lower.Contains("tới đâu") || lower.Contains("hủy đơn") || lower.Contains("huỷ đơn"))
        {
            if (userOrders.Any())
            {
                var latest = userOrders.First();
                var isPending = string.Equals(latest.Status, "pending", StringComparison.OrdinalIgnoreCase);
                var isShipping = string.Equals(latest.Status, "delivering", StringComparison.OrdinalIgnoreCase) || string.Equals(latest.Status, "shipping", StringComparison.OrdinalIgnoreCase);
                var code = latest.OrderCode ?? ("#MV-" + latest.Id);

                if (isPending)
                {
                    reply = $"Milky kiểm tra thấy bạn có đơn hàng **{code}** (trị giá {latest.TotalAmount:N0}đ) đang ở trạng thái **Chờ xác nhận (Pending)**.\n\n"
                          + $"• Bạn có thể theo dõi tiến độ chuẩn bị hoặc tự bấm hủy đơn (trong vòng 2 phút) trực tiếp tại **[Trang Đơn Hàng Của Tôi](/orders)**.\n"
                          + $"• Trong lúc chờ đơn, bạn có muốn xem thêm các món Best-seller bên dưới không ạ?";
                }
                else if (isShipping)
                {
                    reply = $"Tin vui nè! Đơn hàng **{code}** của bạn hiện đang được bác tài giao tới rồi (**Đang giao hàng**)!\n\n"
                          + $"Bạn có thể mở **[Trang Đơn Hàng Của Tôi](/orders)** để xem chi tiết nhé!";
                }
                else
                {
                    reply = $"Đơn hàng gần nhất **{code}** của bạn hiện có trạng thái: **{latest.Status}**.\n\n"
                          + $"Bấm vào **[Trang Đơn Hàng Của Tôi](/orders)** để kiểm tra toàn bộ hóa đơn nha!";
                }
            }
            else
            {
                reply = "Dạ bạn có thể kiểm tra chi tiết hành trình đơn hàng tại trang **[Tra Cứu Đơn Hàng](/orders)** nha!\n\n"
                      + "Hệ thống cập nhật 5 bước thời gian thực:\n"
                      + "1. Chờ xác nhận ➔ 2. Đã xác nhận ➔ 3. Đang pha chế ➔ 4. Đang giao hàng ➔ 5. Hoàn tất.\n\n"
                      + "💡 *Lưu ý:* Bạn có thể tự hủy đơn miễn phí trong vòng 2 phút đầu kể từ lúc đặt hàng!";
            }

            foreach (var p in products.Where(p => p.IsBestSeller).Take(3))
            {
                suggestions.Add(BuildSuggestedDto(p, "Món ngon gợi ý thêm khi bạn chờ đơn hàng."));
            }
        }
        // Kịch bản 7: Béo ngậy / Phô mai / Macchiato / Kem Cheese / Trà sữa đậm vị
        else if (lower.Contains("béo") || lower.Contains("kem") || lower.Contains("cheese") || lower.Contains("phô mai") || lower.Contains("đậm") || lower.Contains("macchiato"))
        {
            var cheeseList = products
                .Where(p => p.Slug.Contains("cheese") || p.Slug.Contains("macchiato") || p.Slug.Contains("kem") || p.Slug.Contains("duong-den") || p.Slug.Contains("matcha"))
                .Take(5)
                .ToList();

            foreach (var item in cheeseList)
            {
                suggestions.Add(BuildSuggestedDto(
                    item,
                    "Lớp kem Cheese Foam béo mặn sánh mịn hòa quyện cùng cốt trà đậm đà.",
                    toppingsOverride: new List<string> { "Kem Phô Mai Macchiato Mặn Ngọt", "Pudding Trứng Vàng Béo Ngậy" }
                ));
            }

            reply = $"Dành cho tín đồ mê đắm vị béo bồng bềnh! Milky lên ngay **Top {suggestions.Count} siêu phẩm Kem Cheese & Trà Sữa béo ngậy** đỉnh nhất MilkyVerse:\n\n"
                  + string.Join("\n", suggestions.Select(s => $"• 🧀 **{s.Name}** ({s.EstimatedPrice:N0}đ): {s.Description}"))
                  + "\n\nBạn chọn ngay các ly bên dưới nhé!";
        }
        // Kịch bản 8: Trái cây thanh mát / Giải khát nhiệt đới
        else if (lower.Contains("chua") || lower.Contains("trái cây") || lower.Contains("thanh nhiệt") || lower.Contains("giải khát") || lower.Contains("nắng") || lower.Contains("mát"))
        {
            var fruitList = products
                .Where(p => p.Slug.Contains("dao") || p.Slug.Contains("xoai") || p.Slug.Contains("tac") || p.Slug.Contains("vai") || p.Slug.Contains("dau") || p.Slug.Contains("cam") || p.Slug.Contains("nhiet-doi"))
                .Take(5)
                .ToList();

            foreach (var item in fruitList)
            {
                suggestions.Add(BuildSuggestedDto(
                    item,
                    "Vị trái cây tươi mọng chua ngọt thanh tao xua tan cơn khát tức thì.",
                    sugarOverride: "50%",
                    iceOverride: "100%",
                    toppingsOverride: new List<string> { "Trân Châu Trắng 3Q Giòn Sần Sật", "Thạch Dừa Giòn" }
                ));
            }

            reply = $"Để đánh bay cảm giác oi bức, Milky gợi ý ngay **Top {suggestions.Count} món Trà Trái Cây Nhiệt Đới** tươi mát chua ngọt hài hòa:\n\n"
                  + string.Join("\n", suggestions.Select(s => $"• 🍹 **{s.Name}** ({s.EstimatedPrice:N0}đ): {s.Description}"))
                  + "\n\nBạn thích vị quả nào nhất để Milky thêm vào giỏ cho bạn nè?";
        }
        // Kịch bản 9: Tìm kiếm khớp tên món hoặc xem toàn bộ Menu / Mặc định đa dạng
        else if (matchedProducts.Any())
        {
            foreach (var mp in matchedProducts)
            {
                suggestions.Add(BuildSuggestedDto(mp, "Khớp trực tiếp với món bạn đang tìm kiếm trong Database."));
            }

            // Nếu tìm thấy ít hơn 4 món thì bổ sung thêm các món cùng loại để khách có nhiều lựa chọn
            if (suggestions.Count < 4)
            {
                var targetType = matchedProducts.First().ProductType ?? "DRINK";
                var more = products
                    .Where(p => (p.ProductType ?? "DRINK") == targetType && suggestions.All(s => s.ProductId != p.Id))
                    .OrderByDescending(p => p.IsBestSeller)
                    .Take(5 - suggestions.Count);
                foreach (var m in more)
                {
                    suggestions.Add(BuildSuggestedDto(m, "Gợi ý cùng nhóm hương vị đang bán chạy."));
                }
            }

            reply = $"Milky tìm thấy **{suggestions.Count} món ngon** trong thực đơn 213 món của MilkyVerse cực hợp với yêu cầu **\"{message}\"** của bạn nè:\n\n"
                  + string.Join("\n", suggestions.Select(p => $"• 🌟 **{p.Name}** ({p.EstimatedPrice:N0}đ): {p.Description}"))
                  + "\n\nBạn có thể bấm **Đặt món này / Thêm giỏ** ở từng thẻ món ngay bên dưới nhé!";
        }
        else
        {
            // Mặc định: Giới thiệu đa dạng 6 món gồm Trà sữa Signature, Trà trái cây, Đồ ăn vặt & Combo
            var topDrinks = products.Where(p => (p.ProductType ?? "DRINK") == "DRINK").OrderByDescending(p => p.IsBestSeller).Take(3);
            var topFoods = products.Where(p => (p.ProductType ?? "DRINK") == "FOOD").OrderByDescending(p => p.IsBestSeller).Take(2);
            var topCombo = products.Where(p => (p.ProductType ?? "DRINK") == "COMBO").Take(1);

            foreach (var d in topDrinks)
            {
                suggestions.Add(BuildSuggestedDto(d, "Thức uống Best-Seller được yêu thích nhất tuần này.", "70%", "70%"));
            }
            foreach (var f in topFoods)
            {
                suggestions.Add(BuildSuggestedDto(f, "Món ăn vặt quốc dân nhâm nhi cùng trà sữa."));
            }
            foreach (var c in topCombo)
            {
                suggestions.Add(BuildSuggestedDto(c, "Combo tiết kiệm kết hợp cả đồ uống & ăn vặt."));
            }

            reply = $"MilkyVerse xin chào bạn! Thực đơn của quán hiện có **213 món** (Trà sữa, Trà trái cây, Kem Cheese, Đá xay, Đồ ăn vặt & Combo). Dưới đây là **Top {suggestions.Count} món Ngôi Sao** bán chạy nhất hôm nay:\n\n"
                  + string.Join("\n", suggestions.Select(s => $"• ✨ **{s.Name}** ({s.EstimatedPrice:N0}đ): {s.Description}"))
                  + "\n\nBạn thích uống trà sữa béo ngậy, trà trái cây thanh nhiệt hay tìm đồ ăn vặt để Milky lọc thêm cho bạn nhé?";
        }

        return new AIChatResponse
        {
            SessionId = sessionId,
            Reply = reply,
            SuggestedDrinks = suggestions,
            IsAiModelLive = false,
            ModelName = "MilkyVerse Smart Dataset API (213 Món)",
            ApiError = apiError
        };
    }
}
