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

        // Load recent orders if user is authenticated
        List<Order> userRecentOrders = new();
        if (userId.HasValue && userId.Value > 0)
        {
            userRecentOrders = await _context.Orders
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.CreatedAt)
                .Take(3)
                .Include(o => o.OrderItems)
                .ToListAsync();
        }

        var geminiApiKey = _config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        AIChatResponse response;

        if (!string.IsNullOrWhiteSpace(geminiApiKey) && geminiApiKey != "YOUR_GEMINI_API_KEY_HERE")
        {
            try
            {
                response = await CallGeminiApiAsync(session.Id, request.Message, activeProducts, activeToppings, userRecentOrders, geminiApiKey);
            }
            catch
            {
                // Graceful fallback to localized barista engine when remote AI fails
                response = FallbackLocalBarista(session.Id, request.Message, activeProducts, activeToppings, userRecentOrders, isErrorFallback: true);
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

    private async Task<AIChatResponse> CallGeminiApiAsync(int sessionId, string userPrompt, List<Product> products, List<Topping> toppings, List<Order> userOrders, string apiKey)
    {
        // Build rich menu categorized dataset summary with dietary tags
        var drinkList = products.Where(p => (p.ProductType ?? "DRINK") == "DRINK").Take(40).Select(p =>
        {
            var isOat = p.Slug.Contains("yen-mach") || p.Slug.Contains("oat");
            var isJuice = p.Slug.Contains("nuoc-ep") || p.Slug.Contains("soda");
            var isNoCaffeine = isJuice || p.Slug.Contains("sua-tuoi");
            var tagStr = isNoCaffeine ? "[Không Caffeine]" : isOat ? "[Thuần Chay/Sữa Yến Mạch]" : "[Có Trà/Caffeine]";
            return $"- [Đồ Uống] ID: {p.Id}, Tên: '{p.Name}', Slug: '{p.Slug}', Giá: {p.BasePrice:N0}đ {tagStr}, Mô tả: {p.Description}";
        });

        var foodList = products.Where(p => (p.ProductType ?? "DRINK") == "FOOD").Take(20).Select(p =>
            $"- [Ăn Vặt] ID: {p.Id}, Tên: '{p.Name}', Slug: '{p.Slug}', Giá: {p.BasePrice:N0}đ, Độ cay: Cấp {p.SpicyLevel ?? 0}, Mô tả: {p.Description}"
        );

        var menuSummary = string.Join("\n", drinkList.Concat(foodList));
        var toppingSummary = string.Join(", ", toppings.Take(15).Select(t => $"{t.Name} (+{t.Price:N0}đ)"));
        var userOrdersSummary = userOrders.Any()
            ? string.Join("; ", userOrders.Select(o => $"Đơn {o.OrderCode ?? ("#MV-" + o.Id)}: trạng thái '{o.Status}', tổng {o.FinalTotal:N0}đ"))
            : "Khách chưa có đơn hàng nào gần đây.";

        var systemPrompt = $@"Bạn là Milky - AI Barista thông minh, chu đáo và tinh tế của thương hiệu trà sữa & ăn vặt MilkyVerse.
Bạn tư vấn bằng tiếng Việt thân thiện, ngọt ngào, chuẩn phong cách MilkyVerse.

THỰC ĐƠN MILKYVERSE HIỆN CÓ:
{menuSummary}

DANH SÁCH TOPPING KHẢ DỤNG:
{toppingSummary}

THÔNG TIN ĐƠN HÀNG GẦN ĐÂY CỦA KHÁCH:
{userOrdersSummary}

QUY TẮC TƯ VẤN BẮT BUỘC (Từ Dataset Huấn Luyện MilkyVerse):
1. Khi khách hỏi món uống buổi tối / sợ mất ngủ / không caffeine: Chỉ gợi ý các món Nước Ép tươi, Soda, hoặc Sữa Tươi Trân Châu Đường Đen (hoàn toàn không chứa caffeine). Tuyệt đối KHÔNG gợi ý trà xanh/hồng trà/ô long/cà phê.
2. Khi khách ăn kiêng, giảm cân, ít ngọt, eat clean: Gợi ý chọn mức 30% đường hoặc 0% đường, đề xuất Trà Oolong mộc hoặc Sữa yến mạch Oatly (thuần chay).
3. Khi khách tìm món mát lạnh dưới 40.000đ: Đề xuất Trà Đào Cam Sả (39.000đ), Trà Tắc Xí Muội (32.000đ) hoặc Nước Ép Dưa Hấu (38.000đ).
4. Khi khách hỏi món quán không bán (như pizza, burger, lẩu...): Lịch sự từ chối quán không có pizza/burger và giới thiệu các món ăn vặt signature cực cuốn: Bánh tráng trộn tôm thịt, Cá viên chiên ngũ sắc, Chân gà sốt Thái, Mì cay 7 cấp độ.
5. Khi khách muốn tự thiết kế hoặc tự pha: Giới thiệu phòng lab Tự Pha Chế tại đường dẫn [/diy](/diy) để tự chọn cốt trà, loại sữa, và topping.
6. Khi khách hỏi về đơn hàng / hủy đơn / tiến độ giao: Dựa vào thông tin đơn hàng ở trên để giải đáp cụ thể và hướng dẫn khách kiểm tra chi tiết tại đường dẫn [/orders](/orders).
7. Luôn dùng đúng tên món trong thực đơn ở trên. Khi gợi ý, kèm theo lời khuyên về % đường, % đá và topping phù hợp nhất.";

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

        // Verify and link products from actual menu mentioned in reply
        var suggestions = new List<SuggestedDrinkDto>();
        foreach (var p in products)
        {
            if (replyText.Contains(p.Name, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(p.Slug) && replyText.Contains(p.Slug, StringComparison.OrdinalIgnoreCase)))
            {
                var isFood = (p.ProductType ?? "DRINK") == "FOOD";
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    ImageUrl = p.ImageUrl,
                    Description = p.Description,
                    EstimatedPrice = p.BasePrice,
                    SugarRecommend = isFood ? "Không áp dụng" : "50%",
                    IceRecommend = isFood ? "Không áp dụng" : "70%",
                    RecommendedToppings = isFood
                        ? new List<string> { p.DefaultSauce ?? "Tương ớt cay nồng" }
                        : new List<string> { "Trân Châu Hoàng Kim Dẻo Dai" },
                    Reason = $"Được Milky đề xuất theo gu ẩm thực của bạn."
                });

                if (suggestions.Count >= 3) break;
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
                    Slug = bestSeller.Slug,
                    ImageUrl = bestSeller.ImageUrl,
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

    private static AIChatResponse FallbackLocalBarista(int sessionId, string message, List<Product> products, List<Topping> toppings, List<Order> userOrders, bool isErrorFallback)
    {
        var lower = message.ToLower().Trim();
        var suggestions = new List<SuggestedDrinkDto>();
        string reply;

        string notice = isErrorFallback
            ? "[Chế độ: AI Barista MilkyVerse - Vận hành tri thức chuyên sâu từ Dataset Huấn Luyện]\n\n"
            : "[Chế độ: AI Barista MilkyVerse - Vận hành tri thức chuyên sâu từ Dataset Huấn Luyện]\n\n";

        // Kịch bản 1: Buổi tối / Không caffeine / Sợ mất ngủ / Trẻ em
        if (lower.Contains("tối") || lower.Contains("mất ngủ") || lower.Contains("không caffeine") || lower.Contains("caffeine") || lower.Contains("không trà"))
        {
            var juice = products.FirstOrDefault(p => p.Slug.Contains("dua-hau") || p.Slug.Contains("nuoc-ep"))
                     ?? products.FirstOrDefault(p => p.Slug.Contains("soda"));
            var freshMilk = products.FirstOrDefault(p => p.Slug.Contains("sua-tuoi") && p.Slug.Contains("duong-den"));

            reply = notice + "Dạ uống buổi tối không lo mất ngủ thì Milky khuyên bạn nên chọn các món thanh mát không chứa caffeine này nha:\n\n"
                  + "• 🍉 **Nước Ép Dưa Hấu Tươi**: Ép tươi nguyên chất, thanh mát ruột gan và rất dễ ngủ.\n"
                  + "• 🥛 **Sữa Tươi Trân Châu Đường Đen**: 100% sữa tươi thanh trùng Dalat béo ngậy kèm trân châu nấu mật dẻo ấm, hoàn toàn không chứa cốt trà!\n\n"
                  + "Bạn muốn Milky chuẩn bị ly nào cho buổi tối thảnh thơi nhé?";

            if (juice != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = juice.Id,
                    Name = juice.Name,
                    Slug = juice.Slug,
                    ImageUrl = juice.ImageUrl,
                    Description = juice.Description,
                    EstimatedPrice = juice.BasePrice,
                    SugarRecommend = "0% hoặc 30%",
                    IceRecommend = "50%",
                    RecommendedToppings = new List<string> { "Thạch Dừa Giòn Băng Tuyết" },
                    Reason = "100% trái cây ép tươi, không caffeine, an tâm thưởng thức buổi tối."
                });
            }

            if (freshMilk != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = freshMilk.Id,
                    Name = freshMilk.Name,
                    Slug = freshMilk.Slug,
                    ImageUrl = freshMilk.ImageUrl,
                    Description = freshMilk.Description,
                    EstimatedPrice = freshMilk.BasePrice,
                    SugarRecommend = "50%",
                    IceRecommend = "50%",
                    RecommendedToppings = new List<string> { "Trân Châu Đen Đường Đen Nướng" },
                    Reason = "Sữa tươi thuần túy không cốt trà, thơm béo ấm bụng."
                });
            }
        }
        // Kịch bản 2: Healthy / Ăn kiêng / Ít ngọt / Giảm cân / Eat clean / Không đường
        else if (lower.Contains("ít ngọt") || lower.Contains("healthy") || lower.Contains("giảm cân") || lower.Contains("không đường") || lower.Contains("eat clean") || lower.Contains("ăn kiêng"))
        {
            var oolong = products.FirstOrDefault(p => p.Slug.Contains("oolong") && !p.Slug.Contains("sua"))
                      ?? products.FirstOrDefault(p => p.Slug.Contains("tra-xanh"));
            var oat = products.FirstOrDefault(p => p.Slug.Contains("yen-mach") || p.Slug.Contains("oat"));

            reply = notice + "Chào bạn yêu thích lối sống lành mạnh! Team Healthy thì chọn ngay các món ít calo và hỗ trợ vóc dáng này của Milky nhé:\n\n"
                  + "• 🍃 **Trà Ô Long Nguyên Chất (0% Đường)**: Cốt trà ủ mộc than hồng, giàu chất chống oxy hóa, hậu ngọt tự nhiên.\n"
                  + "• 🌾 **Trà Sữa Yến Mạch Oatly (30% Đường)**: Sữa yến mạch Thụy Điển thuần chay, không cholesterol, bùi béo tự nhiên mà không gây tăng cân.\n\n"
                  + "Tất cả các dòng trà tại MilkyVerse đều cho phép bạn chỉnh 0% hoặc 30% đường theo ý thích đó ạ!";

            if (oolong != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = oolong.Id,
                    Name = oolong.Name,
                    Slug = oolong.Slug,
                    ImageUrl = oolong.ImageUrl,
                    Description = oolong.Description,
                    EstimatedPrice = oolong.BasePrice,
                    SugarRecommend = "0%",
                    IceRecommend = "50%",
                    RecommendedToppings = new List<string> { "Trân Châu Trắng 3Q Giòn Sần Sật" },
                    Reason = "0 calo từ đường, thanh lọc cơ thể nhẹ nhàng."
                });
            }

            if (oat != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = oat.Id,
                    Name = oat.Name,
                    Slug = oat.Slug,
                    ImageUrl = oat.ImageUrl,
                    Description = oat.Description,
                    EstimatedPrice = oat.BasePrice,
                    SugarRecommend = "30%",
                    IceRecommend = "70%",
                    RecommendedToppings = new List<string> { "Trân Châu Hoàng Kim Nấu Mật" },
                    Reason = "Sữa yến mạch cao cấp lành tính, không lactose."
                });
            }
        }
        // Kịch bản 3: Món rẻ / Dưới 40k / Giá sinh viên / Tiết kiệm
        else if (lower.Contains("dưới 40") || lower.Contains("40k") || lower.Contains("rẻ") || lower.Contains("sinh viên") || lower.Contains("tiết kiệm"))
        {
            var peach = products.FirstOrDefault(p => p.Slug.Contains("dao") || p.Slug.Contains("tac"))
                     ?? products.FirstOrDefault(p => p.BasePrice <= 40000);
            var greenTea = products.FirstOrDefault(p => p.Slug.Contains("lai") || p.Slug.Contains("xanh"));

            reply = notice + "Dạ có ngay các món siêu ngon mát lạnh với giá hạt dẻ dưới 40.000đ cực hot của MilkyVerse nè:\n\n"
                  + "• 🍑 **Trà Đào Cam Sả** (39.000đ): Miếng đào giòn sần sật, sả thơm ngát, giải khát số 1.\n"
                  + "• 🍵 **Trà Xanh Hoa Lài** (35.000đ): Hương lài thơm ngát, giải nhiệt tức thì.\n\n"
                  + "Bạn chọn món nào để Milky lên đơn ngay nhé?";

            if (peach != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = peach.Id,
                    Name = peach.Name,
                    Slug = peach.Slug,
                    ImageUrl = peach.ImageUrl,
                    Description = peach.Description,
                    EstimatedPrice = peach.BasePrice,
                    SugarRecommend = "50%",
                    IceRecommend = "70%",
                    RecommendedToppings = new List<string> { "Đào miếng giòn ngâm mật" },
                    Reason = "Best-seller thanh nhiệt cực kỳ tiết kiệm."
                });
            }

            if (greenTea != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = greenTea.Id,
                    Name = greenTea.Name,
                    Slug = greenTea.Slug,
                    ImageUrl = greenTea.ImageUrl,
                    Description = greenTea.Description,
                    EstimatedPrice = greenTea.BasePrice,
                    SugarRecommend = "50%",
                    IceRecommend = "70%",
                    RecommendedToppings = new List<string> { "Trân Châu Trắng 3Q" },
                    Reason = "Giá chỉ từ 35.000đ thanh mát sảng khoái."
                });
            }
        }
        // Kịch bản 4: Đồ ăn vặt / Đói bụng / Hỏi món quán không bán (pizza, burger, ăn kèm...)
        else if (lower.Contains("pizza") || lower.Contains("burger") || lower.Contains("ăn vặt") || lower.Contains("đói") || lower.Contains("món ăn") || lower.Contains("ăn kèm") || lower.Contains("cá viên") || lower.Contains("bánh tráng"))
        {
            var snack1 = products.FirstOrDefault(p => (p.ProductType ?? "DRINK") == "FOOD" && p.Slug.Contains("banh-trang"))
                      ?? products.FirstOrDefault(p => (p.ProductType ?? "DRINK") == "FOOD");
            var snack2 = products.FirstOrDefault(p => (p.ProductType ?? "DRINK") == "FOOD" && (p.Slug.Contains("ca-vien") || p.Slug.Contains("chan-ga")));

            reply = notice + (lower.Contains("pizza") || lower.Contains("burger")
                ? "Dạ MilkyVerse không phục vụ pizza hay burger ạ! Nhưng quán có thực đơn đồ ăn vặt đường phố nóng hổi cực kỳ đưa miệng để nhâm nhi cùng trà sữa:\n\n"
                : "Uống trà sữa mà có thêm đĩa ăn vặt giòn rụm nóng hổi thì đúng là chuẩn bài luôn bạn ơi! Milky gợi ý ngay các món ăn vặt đắt khách nhất:\n\n")
                  + "• 🥢 **Bánh Tráng Trộn Tôm Thịt Đặc Biệt**: Đậm đà nước sốt bò, trứng cút béo bùi, rau răm xoài chua cay xé lưỡi.\n"
                  + "• 🍢 **Cá Viên Chiên Ngũ Sắc & Chân Gà Sốt Thái**: Chiên nóng giòn rụm chấm tương ớt cay nồng đậm vị!\n\n"
                  + "Bạn có muốn thêm món ăn vặt này vào mâm tiệc hôm nay không nè?";

            if (snack1 != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = snack1.Id,
                    Name = snack1.Name,
                    Slug = snack1.Slug,
                    ImageUrl = snack1.ImageUrl,
                    Description = snack1.Description,
                    EstimatedPrice = snack1.BasePrice,
                    SugarRecommend = "Không áp dụng",
                    IceRecommend = "Không áp dụng",
                    RecommendedToppings = new List<string> { "Nước sốt bò cay đặc chế" },
                    Reason = "Đồ ăn vặt quốc dân kết hợp cực đã cùng trà sữa."
                });
            }

            if (snack2 != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = snack2.Id,
                    Name = snack2.Name,
                    Slug = snack2.Slug,
                    ImageUrl = snack2.ImageUrl,
                    Description = snack2.Description,
                    EstimatedPrice = snack2.BasePrice,
                    SugarRecommend = "Không áp dụng",
                    IceRecommend = "Không áp dụng",
                    RecommendedToppings = new List<string> { "Tương ớt cay nồng" },
                    Reason = "Món chiên nóng giòn thơm nức mũi."
                });
            }
        }
        // Kịch bản 5: Tự pha chế / DIY
        else if (lower.Contains("tự pha") || lower.Contains("diy") || lower.Contains("tự mix") || lower.Contains("công thức"))
        {
            var baseTea = products.FirstOrDefault(p => p.Slug.Contains("oolong") || p.Slug.Contains("matcha"));

            reply = notice + "Bạn hoàn toàn có thể tự tay sáng tạo ly trà sữa mang dấu ấn độc quyền tại phòng lab **DIY Drink Studio** của MilkyVerse nha!\n\n"
                  + "Tại trang [/diy](/diy), bạn được tự do chọn:\n"
                  + "1. Cốt trà: Hồng trà Ceylon, Lục trà lài, Ô long khói, Matcha Kyoto.\n"
                  + "2. Loại sữa: Sữa tươi Dalat, Sữa đặc béo, Sữa yến mạch Oatly hoặc Sữa hạnh nhân.\n"
                  + "3. Lượng đường đá và hơn 20 loại Topping tùy ý!\n\n"
                  + "Bạn có ý tưởng hương vị nào chưa, Milky gợi ý cốt trà cho bạn bấm vào thử nhé: [Mở Phòng Lab Tự Pha Chế](/diy)";

            if (baseTea != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = baseTea.Id,
                    Name = "Trà Sữa Tự Phối DIY Studio",
                    Slug = "diy",
                    Description = "Tự do sáng tạo ly nước của riêng bạn tại phòng lab /diy",
                    EstimatedPrice = 38000,
                    SugarRecommend = "50%",
                    IceRecommend = "70%",
                    RecommendedToppings = new List<string> { "Trân Châu Hoàng Kim Nấu Mật" },
                    Reason = "Mở trang /diy để bắt đầu phối trà và sữa theo phong cách riêng."
                });
            }
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
                    reply = notice + $"Milky kiểm tra thấy bạn có đơn hàng **{code}** (trị giá {latest.FinalTotal:N0}đ) đang ở trạng thái **Chờ xác nhận (Pending)**.\n\n"
                          + $"• Bạn có thể theo dõi tiến độ chuẩn bị hoặc tự bấm hủy đơn (trong vòng 2 phút) trực tiếp tại **[Trang Đơn Hàng Của Tôi](/orders)**.\n"
                          + $"• Bạn có cần Milky hỗ trợ đổi món hay tư vấn gì thêm không ạ?";
                }
                else if (isShipping)
                {
                    reply = notice + $"Tin vui nè! Đơn hàng **{code}** của bạn hiện đang được bác tài giao tới rồi (Đang giao hàng)!\n\n"
                          + $"Bạn có thể mở **[Trang Đơn Hàng Của Tôi](/orders)** để xem vị trí tài xế nhé!";
                }
                else
                {
                    reply = notice + $"Đơn hàng gần nhất **{code}** của bạn hiện có trạng thái: **{latest.Status}**.\n\n"
                          + $"Bấm vào **[Trang Đơn Hàng Của Tôi](/orders)** để kiểm tra chi tiết toàn bộ hóa đơn nha!";
                }
            }
            else
            {
                reply = notice + "Dạ bạn có thể kiểm tra chi tiết hành trình đơn hàng hoặc yêu cầu hỗ trợ đơn nhanh chóng tại trang **[Tra Cứu Đơn Hàng](/orders)** nha!\n\n"
                      + "Tại đó hệ thống sẽ cập nhật 5 bước thời gian thực:\n"
                      + "1. Chờ xác nhận ➔ 2. Đã xác nhận ➔ 3. Đang pha chế ➔ 4. Đang giao hàng ➔ 5. Hoàn tất.\n\n"
                      + "💡 *Lưu ý:* Bạn có thể tự hủy đơn miễn phí trong vòng 2 phút đầu kể từ lúc bấm đặt hàng nhé!";
            }
        }
        // Kịch bản 7: Béo ngậy / Phô mai / Macchiato / Kem Cheese
        else if (lower.Contains("béo") || lower.Contains("kem") || lower.Contains("cheese") || lower.Contains("phô mai") || lower.Contains("đậm"))
        {
            var cheese = products.FirstOrDefault(p => p.Slug.Contains("cheese") || p.Slug.Contains("macchiato"))
                      ?? products.FirstOrDefault(p => p.IsBestSeller);

            reply = notice + "Dành cho tín đồ mê đắm vị béo bồng bềnh! Lớp kem Cheese Foam béo mặn sánh mịn độc quyền của MilkyVerse kết hợp cùng hồng trà đậm đà chắc chắn sẽ làm bạn ngây ngất:";

            if (cheese != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = cheese.Id,
                    Name = cheese.Name,
                    Slug = cheese.Slug,
                    ImageUrl = cheese.ImageUrl,
                    Description = cheese.Description,
                    EstimatedPrice = cheese.BasePrice,
                    SugarRecommend = "50%",
                    IceRecommend = "70%",
                    RecommendedToppings = new List<string> { "Kem Phô Mai Macchiato Mặn Ngọt", "Pudding Trứng Vàng Béo Ngậy" },
                    Reason = "Lớp foam phô mai béo ngậy mặn ngọt hòa quyện cùng cốt trà hảo hạng."
                });
            }
        }
        // Kịch bản 8: Trái cây thanh mát / Giải khát nhiệt đới
        else if (lower.Contains("chua") || lower.Contains("trái cây") || lower.Contains("thanh nhiệt") || lower.Contains("giải khát") || lower.Contains("nắng"))
        {
            var fruit = products.FirstOrDefault(p => p.Slug.Contains("dao") || p.Slug.Contains("xoai") || p.Slug.Contains("tac"))
                     ?? products.FirstOrDefault(p => p.Slug.Contains("trai-cay"));

            reply = notice + "Để đánh bay cảm giác oi bức, một ly trà hoa quả nhiệt đới tươi mát chua ngọt hài hòa sẽ là lựa chọn số 1 hôm nay:";

            if (fruit != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = fruit.Id,
                    Name = fruit.Name,
                    Slug = fruit.Slug,
                    ImageUrl = fruit.ImageUrl,
                    Description = fruit.Description,
                    EstimatedPrice = fruit.BasePrice,
                    SugarRecommend = "50%",
                    IceRecommend = "100%",
                    RecommendedToppings = new List<string> { "Trân Châu Trắng 3Q Giòn Sần Sật" },
                    Reason = "Vị quả tươi mọng chua ngọt thanh tao xua tan cái nóng tức thì."
                });
            }
        }
        // Kịch bản 9: Mặc định / Best-seller
        else
        {
            var sig = products.FirstOrDefault(p => p.IsBestSeller)
                   ?? products.FirstOrDefault(p => p.Slug.Contains("duong-den"));
            var snack = products.FirstOrDefault(p => (p.ProductType ?? "DRINK") == "FOOD");

            reply = notice + "MilkyVerse xin chào bạn! Nếu bạn đang phân vân chưa biết chọn món gì, đây là combo 'ngôi sao' được các fan MilkyVerse yêu thích nhất tuần này:\n\n"
                  + "Bạn thích vị trà đậm đà hay muốn đổi sang một ly trà trái cây thanh nhiệt chua ngọt nè?";

            if (sig != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = sig.Id,
                    Name = sig.Name,
                    Slug = sig.Slug,
                    ImageUrl = sig.ImageUrl,
                    Description = sig.Description,
                    EstimatedPrice = sig.BasePrice,
                    SugarRecommend = "70%",
                    IceRecommend = "70%",
                    RecommendedToppings = new List<string> { "Trân Châu Hoàng Kim Nấu Mật" },
                    Reason = "Công thức kinh điển với hạt trân châu nấu mật ong thơm dẻo bất bại."
                });
            }

            if (snack != null)
            {
                suggestions.Add(new SuggestedDrinkDto
                {
                    ProductId = snack.Id,
                    Name = snack.Name,
                    Slug = snack.Slug,
                    ImageUrl = snack.ImageUrl,
                    Description = snack.Description,
                    EstimatedPrice = snack.BasePrice,
                    SugarRecommend = "Không áp dụng",
                    IceRecommend = "Không áp dụng",
                    RecommendedToppings = new List<string> { "Tương ớt cay nồng" },
                    Reason = "Món ăn vặt giòn ngon chuẩn vị ăn kèm trà sữa."
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
