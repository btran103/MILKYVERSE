using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services.AI;

public interface IMilkyAIOrchestrator
{
    Task<AIChatResponse> ProcessChatAsync(
        int? userId,
        string userRole,
        AIChatRequest request,
        string? resolvedApiKey,
        string resolvedProvider,
        string resolvedModel,
        CancellationToken cancellationToken = default);
}

public class MilkyAIOrchestrator : IMilkyAIOrchestrator
{
    private const int MaxToolCallLoops = 5;

    private readonly MilkyVerseDbContext _db;
    private readonly IGeminiService _geminiService;
    private readonly IToolRegistry _toolRegistry;
    private readonly IToolExecutionService _toolExecutor;
    private readonly IConversationService _conversationService;
    private readonly ILogger<MilkyAIOrchestrator> _logger;
    private readonly Dictionary<string, IAgentTool> _diAgentTools;

    public MilkyAIOrchestrator(
        MilkyVerseDbContext db,
        IGeminiService geminiService,
        IToolRegistry toolRegistry,
        IToolExecutionService toolExecutor,
        IConversationService conversationService,
        ILogger<MilkyAIOrchestrator> logger,
        IEnumerable<IAgentTool>? agentTools = null)
    {
        _db = db;
        _geminiService = geminiService;
        _toolRegistry = toolRegistry;
        _toolExecutor = toolExecutor;
        _conversationService = conversationService;
        _logger = logger;
        _diAgentTools = (agentTools ?? Enumerable.Empty<IAgentTool>())
            .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<AIChatResponse> ProcessChatAsync(
        int? userId,
        string userRole,
        AIChatRequest request,
        string? resolvedApiKey,
        string resolvedProvider,
        string resolvedModel,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(45));
        var ct = timeoutCts.Token;

        // Normalize Windows IME decomposed Unicode (NFD -> NFC) so all Vietnamese strings match reliably
        request.Message = VietnameseNlpHelper.NormalizeNfc(request.Message);

        // 1. Get or create conversation session
        var session = await _conversationService.GetOrCreateSessionAsync(request.SessionId, userId, request.Message);

        // 2. Load user profile info if logged in
        User? user = null;
        if (userId.HasValue && userId.Value > 0)
        {
            user = await _db.Users.FindAsync(userId.Value);
        }

        var effectiveRole = user?.Role ?? (string.IsNullOrWhiteSpace(userRole) ? "Guest" : userRole);

        // 3. Save user message to conversation history
        await _conversationService.SaveUserMessageAsync(session.Id, request.Message);

        // 4. Initialize AgentExecutionContext & synchronize working cart
        var execContext = new AgentExecutionContext
        {
            SessionId = session.Id,
            UserId = userId,
            UserRole = effectiveRole,
            UserName = user?.FullName,
            UserPhone = user?.PhoneNumber,
            UserAddress = user?.Address
        };
        await _toolExecutor.InitializeCartContextAsync(execContext, request.ClientCart);

        // 5. Security check: Prompt Injection / System Prompt Extraction
        if (MilkySystemPrompt.IsPromptInjectionAttempt(request.Message))
        {
            _logger.LogWarning("Blocked prompt injection attempt from UserId={UserId}: {Message}", userId, request.Message);
            var blockedResp = new AIChatResponse
            {
                SessionId = session.Id,
                Reply = "🛡️ **Hệ thống Bảo mật MilkyVerse AI Agent:** Yêu cầu của bạn chứa cú pháp vượt quyền hoặc can thiệp chỉ dẫn hệ thống nên đã bị từ chối. Milky chỉ hỗ trợ các nghiệp vụ tư vấn đồ uống, tự pha chế DIY, giỏ hàng, đơn hàng và báo cáo theo đúng phân quyền tài khoản của bạn nhé!",
                SuggestedPrompts = GetSuggestedPromptsForRole(effectiveRole),
                IsAiModelLive = true,
                ModelName = "MilkyVerse Security Guardrail"
            };
            await _conversationService.SaveAssistantMessageAsync(session.Id, blockedResp);
            return blockedResp;
        }

        var activeMenuCount = await _db.Products.CountAsync(p => p.IsActive, ct);
        var activeToppingCount = await _db.Toppings.CountAsync(t => t.IsAvailable, ct);
        var cartSummary = execContext.WorkingCart.Any()
            ? $"{execContext.WorkingCart.Sum(i => i.Quantity)} món ({string.Join(", ", execContext.WorkingCart.Select(i => $"{i.Quantity}x {i.Name}"))}) - Tạm tính: {execContext.WorkingCart.Sum(i => i.TotalPrice):N0}đ"
            : "Giỏ hàng đang trống";

        var systemPrompt = MilkySystemPrompt.Build(
            effectiveRole,
            user?.FullName,
            userId,
            activeMenuCount,
            activeToppingCount,
            cartSummary);

        // Build multi-turn history from session (excluding the just-added user message)
        var historyTurns = session.Messages
            .OrderByDescending(m => m.CreatedAt)
            .Skip(1)
            .Take(8)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new LlmChatHistoryTurn
            {
                Role = string.Equals(m.SenderRole, "User", StringComparison.OrdinalIgnoreCase) ? "user" : "assistant",
                Content = m.Content
            })
            .ToList();

        AIChatResponse finalResponse;

        // Only call external Gemini/Groq/OpenAI if key looks structurally valid (e.g. AIza..., gsk_..., sk-...)
        bool hasValidLookingKey = !string.IsNullOrWhiteSpace(resolvedApiKey) &&
                                  !resolvedApiKey.StartsWith("AQ.", StringComparison.OrdinalIgnoreCase);

        if (hasValidLookingKey)
        {
            try
            {
                finalResponse = await RunGeminiFunctionCallingLoopAsync(
                    request.Message,
                    systemPrompt,
                    historyTurns,
                    execContext,
                    resolvedApiKey!,
                    resolvedProvider,
                    resolvedModel,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External LLM Agent loop failed; falling back to Smart Local Agent Engine.");
                finalResponse = await RunLocalAgentEngineAsync(request.Message, execContext, isFallback: true, apiError: ex.Message, ct);
            }
        }
        else
        {
            finalResponse = await RunLocalAgentEngineAsync(request.Message, execContext, isFallback: false, apiError: null, ct);
        }

        // Persist assistant response with full structured payload
        await _conversationService.SaveAssistantMessageAsync(session.Id, finalResponse);
        return finalResponse;
    }

    private async Task<AIChatResponse> RunGeminiFunctionCallingLoopAsync(
        string userMessage,
        string systemPrompt,
        List<LlmChatHistoryTurn> historyTurns,
        AgentExecutionContext execContext,
        string apiKey,
        string provider,
        string model,
        CancellationToken ct)
    {
        var toolExecutions = new List<LlmToolResponseItem>();
        string finalReply = string.Empty;
        string usedModel = model;

        for (int loop = 0; loop < MaxToolCallLoops; loop++)
        {
            var turnReq = new LlmTurnRequest
            {
                ApiKey = apiKey,
                Provider = provider,
                Model = usedModel,
                SystemPrompt = systemPrompt,
                UserMessage = userMessage,
                History = historyTurns,
                PreviousToolExecutions = toolExecutions,
                UserRole = execContext.UserRole
            };

            var turnRes = await _geminiService.GenerateTurnAsync(turnReq, ct);
            if (!turnRes.Success)
            {
                throw new Exception(turnRes.ErrorMessage ?? "LLM call failed.");
            }

            usedModel = turnRes.UsedModel;

            if (!turnRes.HasFunctionCalls)
            {
                finalReply = turnRes.TextReply;
                break;
            }

            // Execute every requested function call in sequence (first check modular DI IAgentTool, then ToolExecutionService)
            foreach (var fnCall in turnRes.FunctionCalls)
            {
                if (_diAgentTools.TryGetValue(fnCall.Name, out var agentTool))
                {
                    var agentCtx = new AgentToolContext
                    {
                        SessionId = execContext.SessionId,
                        UserId = execContext.UserId,
                        UserRole = execContext.UserRole,
                        SuggestedDrinks = execContext.SuggestedDrinks,
                        Actions = execContext.Actions,
                        ExecutedTools = execContext.ExecutedTools
                    };

                    var toolOut = await agentTool.ExecuteAsync(fnCall.Arguments, agentCtx, ct);
                    execContext.ExecutedTools.Add(new AIToolExecutionLogDto
                    {
                        ToolName = agentTool.Name,
                        ArgumentsJson = fnCall.RawArgumentsJson,
                        ResultSummary = toolOut.Summary,
                        Success = toolOut.Success
                    });

                    toolExecutions.Add(new LlmToolResponseItem
                    {
                        CallId = fnCall.CallId,
                        Name = fnCall.Name,
                        ArgumentsJson = fnCall.RawArgumentsJson,
                        ResponseData = new
                        {
                            success = toolOut.Success,
                            summary = toolOut.Summary,
                            data = toolOut.Data
                        }
                    });
                }
                else
                {
                    var toolResult = await _toolExecutor.ExecuteToolAsync(fnCall.Name, fnCall.Arguments, execContext, ct);
                    toolExecutions.Add(new LlmToolResponseItem
                    {
                        CallId = fnCall.CallId,
                        Name = fnCall.Name,
                        ArgumentsJson = fnCall.RawArgumentsJson,
                        ResponseData = new
                        {
                            success = toolResult.Success,
                            summary = toolResult.Summary,
                            data = toolResult.Data
                        }
                    });
                }
            }
        }

        // SuggestedDrinks are strictly populated from Tool results (real ProductIds), never from regex/string matching on LLM reply text.
        if (string.IsNullOrWhiteSpace(finalReply))
        {
            finalReply = BuildSynthesizedReplyFromContext(execContext);
        }

        return BuildFinalChatResponse(execContext, finalReply, isLive: true, modelName: $"{provider} ({usedModel})", apiError: null);
    }

    private async Task EnrichWithImplicitToolsIfNeededAsync(
        string userMessage,
        string replyText,
        AgentExecutionContext execContext,
        CancellationToken ct)
    {
        var activeProducts = await _db.Products.Include(p => p.Category).Where(p => p.IsActive && p.StockStatus == 1).ToListAsync(ct);
        var normalizedReply = VietnameseNlpHelper.NormalizeNfc(replyText);

        foreach (var p in activeProducts)
        {
            var pName = VietnameseNlpHelper.NormalizeNfc(p.Name);
            if (normalizedReply.Contains(pName, StringComparison.OrdinalIgnoreCase) &&
                execContext.SuggestedDrinks.All(s => s.ProductId != p.Id))
            {
                execContext.SuggestedDrinks.Add(new SuggestedDrinkDto
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    ImageUrl = p.ImageUrl,
                    Description = p.Description,
                    EstimatedPrice = p.BasePrice,
                    ProductType = p.ProductType ?? "DRINK",
                    CategoryName = p.Category?.Name ?? "",
                    IsInStock = true,
                    Reason = "Được Milky AI đề xuất phù hợp với yêu cầu của bạn."
                });
                if (execContext.SuggestedDrinks.Count >= 6) break;
            }
        }
    }

    // =========================================================================
    // SMART LOCAL AGENT ENGINE (Handles ANY Free-Form Vietnamese Prompt -> Real Tools)
    // =========================================================================
    private async Task<AIChatResponse> RunLocalAgentEngineAsync(
        string rawMessage,
        AgentExecutionContext execContext,
        bool isFallback,
        string? apiError,
        CancellationToken ct)
    {
        var message = VietnameseNlpHelper.NormalizeNfc(rawMessage);
        var lower = message.ToLowerInvariant();
        var ascii = VietnameseNlpHelper.RemoveDiacritics(message);
        var replyBuilder = new StringBuilder();

        var activeProducts = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        // ---------------------------------------------------------------------
        // 1. ADMIN & BUSINESS ANALYTICS INTENT
        // ---------------------------------------------------------------------
        if (ContainsAny(ascii, "doanh thu", "thong ke don", "bao cao doanh thu", "phan tich kinh doanh", "thong ke khach hang", "top khach hang", "bao cao admin"))
        {
            var salesRes = await _toolExecutor.ExecuteToolAsync("GetSalesAnalytics", new(), execContext, ct);
            if (!salesRes.Success)
            {
                replyBuilder.AppendLine($"⚠️ **{salesRes.Summary}**");
                replyBuilder.AppendLine("Chức năng phân tích doanh thu và quản trị kinh doanh chỉ dành cho tài khoản **Admin / Manager**.");
            }
            else
            {
                await _toolExecutor.ExecuteToolAsync("GetBestSellingProducts", new(), execContext, ct);
                await _toolExecutor.ExecuteToolAsync("GetCustomerStatistics", new(), execContext, ct);
                replyBuilder.AppendLine("📊 **Báo Cáo Phân Tích Kinh Doanh MilkyVerse (Dữ liệu thời gian thực):**\n");
                replyBuilder.AppendLine($"• **Doanh thu ghi nhận:** `{execContext.AdminAnalytics?.TotalRevenue:N0}đ`");
                replyBuilder.AppendLine($"• **Tổng số đơn hàng:** `{execContext.AdminAnalytics?.TotalOrders}` đơn (`{execContext.AdminAnalytics?.CompletedOrders}` hoàn tất/đã thanh toán, `{execContext.AdminAnalytics?.PendingOrders}` đang xử lý, `{execContext.AdminAnalytics?.CancelledOrders}` đã hủy)");
                replyBuilder.AppendLine($"• **Tổng số khách hàng:** `{execContext.AdminAnalytics?.TotalCustomers}` khách hàng");
                replyBuilder.AppendLine($"• **Cảnh báo tồn kho:** `{execContext.AdminAnalytics?.LowStockCount}` nguyên liệu dưới định mức\n");
                replyBuilder.AppendLine("Dưới đây là thẻ tổng hợp số liệu và danh sách sản phẩm bán chạy nhất:");
            }
            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        // ---------------------------------------------------------------------
        // 2. WAREHOUSE & INVENTORY REPORT INTENT
        // ---------------------------------------------------------------------
        if (ContainsAny(ascii, "nguyen lieu nao sap het", "bao cao kho", "kiem tra ton kho", "ton kho nguyen lieu", "sap het hang", "kiem kho"))
        {
            var lowRes = await _toolExecutor.ExecuteToolAsync("GetLowStockIngredients", new(), execContext, ct);
            if (!lowRes.Success)
            {
                replyBuilder.AppendLine($"⚠️ **{lowRes.Summary}**");
            }
            else
            {
                await _toolExecutor.ExecuteToolAsync("GetInventoryReport", new(), execContext, ct);
                replyBuilder.AppendLine($"📦 **Báo Cáo Tồn Kho Nguyên Vật Liệu MilkyVerse:**\n\n{execContext.InventoryReport?.SummaryNote}");
                if (execContext.InventoryReport?.HighlightedIngredients.Any() == true)
                {
                    replyBuilder.AppendLine("\n**Danh sách nguyên liệu cần chú ý:**");
                    foreach (var ing in execContext.InventoryReport.HighlightedIngredients.Take(8))
                    {
                        replyBuilder.AppendLine($"• **{ing.Name}**: Tồn hiện tại `{ing.CurrentStock:N0} {ing.Unit}` (Ngưỡng cảnh báo: `{ing.MinAlertThreshold:N0} {ing.Unit}`)");
                    }
                }
            }
            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        // ---------------------------------------------------------------------
        // 3. INGREDIENT / STOCK AVAILABILITY CHECK ("Món X còn hàng/nguyên liệu không?")
        // ---------------------------------------------------------------------
        if (ContainsAny(ascii, "con nguyen lieu khong", "con hang khong", "con ban khong", "het hang chua"))
        {
            var matchedProd = VietnameseNlpHelper.FindBestMatchingProduct(message, activeProducts);
            var prodName = matchedProd?.Name ?? "Trà sữa trân châu đen";
            var availRes = await _toolExecutor.ExecuteToolAsync("CheckIngredientAvailability", CreateJsonArgs(("productName", prodName)), execContext, ct);
            replyBuilder.AppendLine($"🔍 **Kiểm tra nguyên liệu pha chế thực tế:**\n\n• {availRes.Summary}");
            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        // ---------------------------------------------------------------------
        // 4. CART CLEAR / REMOVE / UPDATE / VIEW INTENTS
        // ---------------------------------------------------------------------
        if (ContainsAny(ascii, "xoa toan bo gio", "lam trong gio", "xoa het gio", "clear cart", "huy gio hang"))
        {
            var res = await _toolExecutor.ExecuteToolAsync("ClearCart", new(), execContext, ct);
            replyBuilder.AppendLine($"🛒 **{res.Summary}**");
            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        if (ContainsAny(ascii, "xoa mon", "bo mon", "xoa khoi gio", "bo khoi gio", "xoa ly"))
        {
            var matchedProd = VietnameseNlpHelper.FindBestMatchingProduct(message, activeProducts);
            var targetName = matchedProd?.Name ?? execContext.WorkingCart.FirstOrDefault()?.Name ?? "";
            var res = await _toolExecutor.ExecuteToolAsync("RemoveCartItem", CreateJsonArgs(("productName", targetName)), execContext, ct);
            replyBuilder.AppendLine($"🛒 **{res.Summary}**");
            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        if (ContainsAny(ascii, "sua gio hang", "cap nhat gio", "doi so luong", "tang so luong", "giam so luong") ||
            (ascii.Contains("trong gio") && ContainsAny(ascii, "doi", "sua", "thanh")))
        {
            var matchedProd = VietnameseNlpHelper.FindBestMatchingProduct(message, activeProducts);
            var targetName = matchedProd?.Name ?? execContext.WorkingCart.FirstOrDefault()?.Name ?? "";
            var newQty = VietnameseNlpHelper.ExtractClauseQuantity(ascii);
            var res = await _toolExecutor.ExecuteToolAsync("UpdateCartItem", CreateJsonArgs(
                ("productName", targetName),
                ("newQuantity", newQty)
            ), execContext, ct);
            replyBuilder.AppendLine($"🛒 **{res.Summary}**");
            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        if (ContainsAny(ascii, "xem gio hang", "gio hang cua toi", "gio hang cua tui", "trong gio co gi", "kiem tra gio hang", "gio hang hien tai"))
        {
            var res = await _toolExecutor.ExecuteToolAsync("GetCart", new(), execContext, ct);
            replyBuilder.AppendLine($"🛍️ **{res.Summary}**");
            if (execContext.WorkingCart.Any())
            {
                foreach (var item in execContext.WorkingCart)
                {
                    replyBuilder.AppendLine($"• **{item.Quantity}x {item.Name}** (Size {item.Size}{(string.IsNullOrEmpty(item.SugarLevel) ? "" : $", {item.SugarLevel} đường")}) — `{item.TotalPrice:N0}đ`");
                }
            }
            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        // ---------------------------------------------------------------------
        // 5. COUPON / VOUCHER VALIDATION INTENT ("kiểm tra mã MILKYNEW", "áp mã FREESHIP")
        // ---------------------------------------------------------------------
        if (ContainsAny(ascii, "ma giam gia", "voucher", "coupon", "ap ma", "milkynew", "freeship", "aibarista", "milky20"))
        {
            var codeMatch = Regex.Match(message, @"\b(MILKYNEW|FREESHIP|AIBARISTA|MILKY\w+)\b", RegexOptions.IgnoreCase);
            var code = codeMatch.Success ? codeMatch.Groups[1].Value.ToUpperInvariant() : "MILKYNEW";
            var subtotal = execContext.WorkingCart.Any() ? execContext.WorkingCart.Sum(i => i.TotalPrice) : 120000m;

            var couponTool = new ValidateCouponTool(new OrderService(_db));
            var agentCtx = new AgentToolContext
            {
                SessionId = execContext.SessionId,
                UserId = execContext.UserId,
                UserRole = execContext.UserRole,
                SuggestedDrinks = execContext.SuggestedDrinks,
                Actions = execContext.Actions,
                ExecutedTools = execContext.ExecutedTools
            };

            var couponRes = await couponTool.ExecuteAsync(CreateJsonArgs(("code", code), ("subtotal", subtotal)), agentCtx, ct);
            execContext.ExecutedTools.Add(new AIToolExecutionLogDto
            {
                ToolName = "validate_coupon",
                ArgumentsJson = $"{{\"code\":\"{code}\",\"subtotal\":{subtotal}}}",
                ResultSummary = couponRes.Summary,
                Success = couponRes.Success
            });

            replyBuilder.AppendLine(couponRes.Success
                ? $"🎟️ **{couponRes.Summary}**\nMilky đã áp dụng sẵn mã **`{code}`** cho bạn. Bạn có thể chuyển sang trang **[Thanh toán (/checkout)](/checkout)** để hoàn tất đơn hàng nhé!"
                : $"⚠️ **{couponRes.Summary}**");

            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        // ---------------------------------------------------------------------
        // 5b. CHECKOUT / PLACE ORDER GUIDANCE (Agent does NOT place orders or pay directly; guides customer to /checkout)
        // ---------------------------------------------------------------------
        if (ContainsAny(ascii, "xac nhan dat don", "tao don hang", "chot don giao", "dat hang ngay", "len don cho tui", "len don cho toi") &&
            !ContainsAny(ascii, "them", "lay 1", "lay 2"))
        {
            replyBuilder.AppendLine("🛍️ Để đảm bảo an toàn thông tin giao hàng và thanh toán của bạn, Milky không tự động chốt đơn thay khách hàng.\n\nBạn vui lòng bấm chuyển sang trang **[Thanh Toán Đơn Hàng (/checkout)](/checkout)** để kiểm tra lại địa chỉ nhận hàng và chọn phương thức thanh toán (COD / VietQR SePay) nhé!");
            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        // ---------------------------------------------------------------------
        // 6. FLEXIBLE MULTI-ITEM / MULTI-CONFIGURATION ADD TO CART INTENT
        //    Handles ANY phrasing:
        //    - "thêm cho tui 1 trà sữa truyền thống, 1 cá viên chiên"
        //    - "Thêm cho tôi 2 ly trà sữa matcha, 1 ly ít đường và 1 ly bình thường"
        //    - "lấy mình 2 trà đào cam sả size L ít đá với 1 bánh tráng trộn"
        //    - "cho tui 1 hồng trà kem cheese"
        //    - "mua 1 mì cay hải sản, 1 sữa tươi trân châu đường đen"
        // ---------------------------------------------------------------------
        bool hasOrderVerb = Regex.IsMatch(ascii, @"\b(them|lay|mua|dat|order|bo vao gio|cho tui|cho toi|cho minh|cho em|chot)\b");
        bool isPureQuestion = ContainsAny(ascii, "gia bao nhieu", "la gi", "co ngon khong", "con nguyen lieu", "tu pha", "cong thuc");

        if (hasOrderVerb && !isPureQuestion)
        {
            var parsedClauses = VietnameseNlpHelper.ParseMultiItemCartRequest(message, activeProducts);
            if (parsedClauses.Count > 0)
            {
                var addedSummaries = new List<string>();
                foreach (var clause in parsedClauses)
                {
                    var addRes = await _toolExecutor.ExecuteToolAsync("AddToCart", CreateJsonArgs(
                        ("productId", clause.Product.Id),
                        ("productName", clause.Product.Name),
                        ("size", clause.Size),
                        ("sugarLevel", clause.SugarLevel),
                        ("iceLevel", clause.IceLevel),
                        ("spicyLevel", clause.SpicyLevel),
                        ("quantity", clause.Quantity),
                        ("toppingNames", string.Join(",", clause.Toppings))
                    ), execContext, ct);

                    if (addRes.Success)
                    {
                        var configDesc = clause.Product.ProductType == "FOOD"
                            ? $"Phần tiêu chuẩn ({clause.SpicyLevel})"
                            : $"Size {clause.Size}, {clause.SugarLevel} đường, {clause.IceLevel} đá";
                        if (clause.Toppings.Any())
                        {
                            configDesc += $", +{string.Join(", ", clause.Toppings)}";
                        }
                        addedSummaries.Add($"• **{clause.Quantity}x {clause.Product.Name}** — *{configDesc}* (`{clause.Product.BasePrice:N0}đ`/phần)");
                    }
                }

                if (addedSummaries.Any())
                {
                    replyBuilder.AppendLine($"✅ Milky đã thực thi công cụ **`AddToCart`** ({addedSummaries.Count} cấu hình) và cập nhật trực tiếp vào giỏ hàng của bạn:\n");
                    foreach (var line in addedSummaries)
                    {
                        replyBuilder.AppendLine(line);
                    }
                    replyBuilder.AppendLine($"\n💰 **Tổng giỏ hàng hiện tại:** `{execContext.WorkingCart.Sum(i => i.Quantity)}` món — **`{execContext.WorkingCart.Sum(i => i.TotalPrice):N0}đ`**.");
                    return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
                }
            }
        }

        // ---------------------------------------------------------------------
        // 7. ORDER TRACKING / HISTORY / PAYMENT GUIDANCE INTENT
        // ---------------------------------------------------------------------
        if (ContainsAny(ascii, "don hang", "tra cuu don", "theo doi don", "lich su don", "kiem tra don", "thanh toan don", "ma qr", "phuong thuc thanh toan"))
        {
            if (ContainsAny(ascii, "phuong thuc thanh toan", "thanh toan bang cach nao", "huong dan thanh toan"))
            {
                var payGuide = await _toolExecutor.ExecuteToolAsync("GetPaymentGuidance", new(), execContext, ct);
                replyBuilder.AppendLine("💳 **Các phương thức thanh toán chính thức tại MilkyVerse:**\n");
                replyBuilder.AppendLine("1. **VietQR TPBank tự động (SePay):** Quét mã QR ngân hàng, hệ thống Webhook tự động xác nhận đơn ngay khi nhận tiền.");
                replyBuilder.AppendLine("2. **Tiền mặt khi nhận hàng (COD):** Thanh toán trực tiếp cho Shipper khi nhận đồ uống.");
                replyBuilder.AppendLine("3. **Ví điện tử MoMo:** Thanh toán nhanh qua cổng MoMo.");
                return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
            }

            var codeMatch = Regex.Match(message, @"MV\d{8}[A-Z0-9]+", RegexOptions.IgnoreCase);
            if (codeMatch.Success)
            {
                var detailRes = await _toolExecutor.ExecuteToolAsync("TrackOrder", CreateJsonArgs(("orderCode", codeMatch.Value)), execContext, ct);
                replyBuilder.AppendLine($"📦 **Kết quả tra cứu đơn hàng #{codeMatch.Value}:**\n\n• {detailRes.Summary}");
                if (ContainsAny(ascii, "thanh toan", "qr"))
                {
                    await _toolExecutor.ExecuteToolAsync("CreatePaymentTransaction", CreateJsonArgs(("orderCode", codeMatch.Value)), execContext, ct);
                }
            }
            else
            {
                var histRes = await _toolExecutor.ExecuteToolAsync("GetOrderHistory", CreateJsonArgs(("limit", 5)), execContext, ct);
                if (!histRes.Success)
                {
                    replyBuilder.AppendLine($"ℹ️ {histRes.Summary}");
                }
                else if (execContext.Orders.Any())
                {
                    var latest = execContext.Orders.First();
                    replyBuilder.AppendLine($"📦 Milky tìm thấy **{execContext.Orders.Count} đơn hàng** gần đây của bạn. Đơn mới nhất là **#{latest.OrderCode}** (Trạng thái: **{latest.Status}**, Thanh toán: **{latest.PaymentStatus}**, Tổng: `{latest.TotalAmount:N0}đ`).");
                }
                else
                {
                    replyBuilder.AppendLine("📦 Hiện tại tài khoản của bạn chưa có đơn hàng nào. Bạn hãy chọn món ngon để Milky hỗ trợ lên đơn nhé!");
                }
            }
            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        // ---------------------------------------------------------------------
        // 8. TOPPINGS LIST / DIY DRINK STUDIO / RECIPE GENERATION INTENT
        // ---------------------------------------------------------------------
        if (ContainsAny(ascii, "co nhung topping gi", "danh sach topping", "topping gia", "cac loai topping"))
        {
            await _toolExecutor.ExecuteToolAsync("GetAvailableToppings", new(), execContext, ct);
            var toppings = await _db.Toppings.Where(t => t.IsAvailable).OrderBy(t => t.Price).ToListAsync(ct);
            replyBuilder.AppendLine($"🧋 **Danh sách {toppings.Count} loại Topping đang có sẵn tại MilkyVerse:**\n");
            foreach (var t in toppings)
            {
                replyBuilder.AppendLine($"• **{t.Name}** — `+{t.Price:N0}đ`");
            }
            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        if (ContainsAny(ascii, "tu pha", "diy", "cong thuc", "tuy chinh ly", "phoi tra", "tinh gia ly", "luu cong thuc"))
        {
            bool isSave = ascii.Contains("luu cong thuc");
            bool isReference = ascii.Contains("tham khao") || ascii.Contains("tai nha");
            var size = Regex.IsMatch(ascii, @"\bsize\s*l\b") ? "L" : "M";
            var sugar = ascii.Contains("0%") || ascii.Contains("khong duong") ? "0%"
                      : ascii.Contains("30%") || ascii.Contains("it ngot") || ascii.Contains("it duong") ? "30%" : "50%";
            var ice = ascii.Contains("0% da") || ascii.Contains("khong da") ? "0%" : "70%";
            var toppings = ExtractMentionedToppings(ascii);

            await _toolExecutor.ExecuteToolAsync("GenerateRecipe", CreateJsonArgs(
                ("themeOrCraving", message),
                ("teaBaseId", message),
                ("milkId", message),
                ("size", size),
                ("sugarLevel", sugar),
                ("iceLevel", ice),
                ("toppingNames", string.Join(",", toppings)),
                ("isReferenceOnly", isReference)
            ), execContext, ct);

            await _toolExecutor.ExecuteToolAsync("CustomizeDrink", CreateJsonArgs(
                ("isCustomDIY", true),
                ("teaBaseId", message),
                ("milkId", message),
                ("size", size),
                ("sugarLevel", sugar),
                ("iceLevel", ice),
                ("toppingNames", string.Join(",", toppings)),
                ("quantity", 1)
            ), execContext, ct);

            if (isSave)
            {
                await _toolExecutor.ExecuteToolAsync("SaveFavoriteRecipe", CreateJsonArgs(
                    ("recipeName", $"DIY Signature ({size}, {sugar} đường)"),
                    ("teaBaseId", message),
                    ("milkId", message),
                    ("size", size),
                    ("sugarLevel", sugar),
                    ("iceLevel", ice),
                    ("toppingNames", string.Join(",", toppings))
                ), execContext, ct);
            }

            var recipe = execContext.Recipes.FirstOrDefault();
            var diy = execContext.Customizations.FirstOrDefault();
            if (recipe != null)
            {
                replyBuilder.AppendLine($"🧪 **Công thức DIY: {recipe.RecipeName}** ({(recipe.IsReferenceFormula ? "Tham khảo tại nhà" : "Cấu hình đặt pha chế ngay")}):");
                replyBuilder.AppendLine($"• **Thành phần:** {recipe.TeaBaseName} + {recipe.MilkName} | Size {recipe.Size} | {recipe.SugarLevel} đường | {recipe.IceLevel} đá");
                if (recipe.Toppings.Any())
                    replyBuilder.AppendLine($"• **Topping:** {string.Join(", ", recipe.Toppings.Select(t => t.Name))}");
                if (diy != null)
                    replyBuilder.AppendLine($"• **Giá backend tính:** `{diy.UnitPrice:N0}đ`");
                if (isSave)
                    replyBuilder.AppendLine("💾 Đã lưu công thức vào danh sách yêu thích của bạn!");
            }

            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse Local Agent Engine (31 Tools)", apiError);
        }

        // ---------------------------------------------------------------------
        // 9, 10, 11, 12: DYNAMIC 213-ITEM SEMANTIC SEARCH & FILTER ENGINE
        // Queries ALL 213 products & 23 categories based on the customer's actual request
        // (supports specific flavors/dishes, OR-queries, price ranges, dietary needs, weather/mood)
        // ---------------------------------------------------------------------

        // 10. Specific single product detail lookup ("Món X giá bao nhiêu?", "Chi tiết món X")
        if (ContainsAny(ascii, "gia bao nhieu", "bao nhieu tien", "chi tiet mon", "thong tin mon"))
        {
            var matchedProd = VietnameseNlpHelper.FindBestMatchingProduct(message, activeProducts);
            if (matchedProd != null)
            {
                await _toolExecutor.ExecuteToolAsync("GetProductDetails", CreateJsonArgs(("productId", matchedProd.Id)), execContext, ct);
                replyBuilder.AppendLine($"🔎 **Thông tin chi tiết món `{matchedProd.Name}`** (Danh mục: *{matchedProd.Category?.Name}*):\n");
                replyBuilder.AppendLine($"• **Giá gốc (Size M):** `{matchedProd.BasePrice:N0}đ` {(matchedProd.ProductType == "DRINK" ? $"(Size L: `{(matchedProd.BasePrice + 8000):N0}đ`)" : "")}");
                replyBuilder.AppendLine($"• **Mô tả:** {matchedProd.Description}");
                replyBuilder.AppendLine($"• **Trạng thái:** {(matchedProd.StockStatus == 1 ? "✅ Đang kinh doanh & sẵn sàng phục vụ" : "⚠️ Tạm hết hàng")}");
                return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse AI Agent Engine (213 Items Dynamic Search)", apiError);
            }
        }

        // Handle customer asking for non-existent items like pizza, burger, lẩu, bún bò...
        if (ContainsAny(ascii, "pizza", "burger", "hamburger", "pho bo", "bun bo", "hu tieu", "com tam", "lau "))
        {
            var snacks = activeProducts
                .Where(p => p.IsActive && p.StockStatus == 1 && p.ProductType == "FOOD")
                .OrderByDescending(p => p.IsBestSeller)
                .Take(6)
                .ToList();

            foreach (var sn in snacks)
            {
                execContext.SuggestedDrinks.Add(new SuggestedDrinkDto
                {
                    ProductId = sn.Id,
                    Name = sn.Name,
                    Slug = sn.Slug,
                    ImageUrl = sn.ImageUrl,
                    Description = sn.Description,
                    EstimatedPrice = sn.BasePrice,
                    ProductType = sn.ProductType ?? "FOOD",
                    CategoryName = sn.Category?.Name ?? "",
                    IsInStock = true,
                    SugarRecommend = "Không áp dụng",
                    IceRecommend = "Không áp dụng",
                    RecommendedToppings = new List<string> { sn.DefaultSauce },
                    Reason = "Món ăn vặt thay thế cực ngon có sẵn tại MilkyVerse."
                });
            }

            execContext.ExecutedTools.Add(new AIToolExecutionLogDto
            {
                ToolName = "search_menu",
                ArgumentsJson = JsonSerializer.Serialize(new { productType = "FOOD", limit = 6 }),
                ResultSummary = $"Từ chối lịch sự món không bán và gợi ý {snacks.Count} món ăn vặt có sẵn.",
                Success = true
            });

            replyBuilder.AppendLine("Dạ MilkyVerse rất tiếc là quán hiện chưa phục vụ các món đó ạ! Nhưng quán có nguyên thực đơn **37 món ăn vặt nóng giòn** cực kỳ hấp dẫn để bạn lựa chọn:\n");
            foreach (var s in execContext.SuggestedDrinks)
            {
                replyBuilder.AppendLine($"• 🍟 **{s.Name}** — **`{s.EstimatedPrice:N0}đ`**: {s.Description}");
            }
            return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse AI Agent Engine (213 Items Dynamic Search)", apiError);
        }

        // Dynamic Semantic Search across ALL 213 items
        var intent = VietnameseNlpHelper.AnalyzeMenuQuery(message);
        var rankedMatches = VietnameseNlpHelper.ScoreAndRankProducts(activeProducts, intent, message);

        // Log exact dynamic search tool execution
        var searchToolName = (intent.MinPrice.HasValue || intent.MaxPrice.HasValue) ? "FilterProductsByPrice" : "search_menu";
        execContext.ExecutedTools.Add(new AIToolExecutionLogDto
        {
            ToolName = searchToolName,
            ArgumentsJson = JsonSerializer.Serialize(new
            {
                query = string.Join(" ", intent.MeaningfulKeywords),
                minPrice = intent.MinPrice,
                maxPrice = intent.MaxPrice,
                productType = intent.ProductType,
                noCaffeine = intent.NoCaffeine,
                vegan = intent.VeganOrNoMilk,
                totalCandidatesMatched = rankedMatches.Count
            }),
            ResultSummary = $"Tìm thấy {rankedMatches.Count} món khớp từ kho thực đơn 213 món.",
            Success = true
        });

        // If user asks for full menu overview without specific constraints, showcase diverse items across categories
        List<(Product Product, int Score, string MatchReason)> displayList;
        if (rankedMatches.Count == 0 && ContainsAny(ascii, "menu", "thuc don", "tat ca mon", "co gi ngon", "ban gi"))
        {
            displayList = activeProducts
                .Where(p => p.IsActive && p.StockStatus == 1)
                .GroupBy(p => p.CategoryId)
                .Select(g => g.OrderByDescending(p => p.IsBestSeller).First())
                .Take(8)
                .Select(p => (p, 10, $"Đại diện nhóm {p.Category?.Name}"))
                .ToList();
        }
        else if (rankedMatches.Count > 0)
        {
            displayList = rankedMatches.Take(6).ToList();
        }
        else
        {
            // No exact match across 213 items -> politely inform user AND suggest closest/diverse drinks by category instead of fixed 5 items
            displayList = activeProducts
                .Where(p => p.IsActive && p.StockStatus == 1)
                .GroupBy(p => p.CategoryId)
                .Select(g => g.First())
                .Take(6)
                .Select(p => (p, 5, $"Gợi ý đa dạng từ nhóm {p.Category?.Name}"))
                .ToList();
        }

        string recSugar = intent.LowSugar ? "30% (Ít ngọt)" : intent.WarmOrNoIce ? "50% (Dịu nhẹ)" : "50%";
        string recIce = intent.WarmOrNoIce ? "0% Đá (Uống ấm)" : intent.NoCaffeine ? "50%" : "70%";

        foreach (var (prod, _, matchReason) in displayList)
        {
            var isFood = string.Equals(prod.ProductType, "FOOD", StringComparison.OrdinalIgnoreCase);
            execContext.SuggestedDrinks.Add(new SuggestedDrinkDto
            {
                ProductId = prod.Id,
                Name = prod.Name,
                Slug = prod.Slug,
                ImageUrl = prod.ImageUrl,
                Description = prod.Description,
                EstimatedPrice = prod.BasePrice,
                ProductType = prod.ProductType ?? "DRINK",
                CategoryName = prod.Category?.Name ?? "",
                IsInStock = prod.IsActive && prod.StockStatus == 1,
                SugarRecommend = isFood ? "Không áp dụng" : recSugar,
                IceRecommend = isFood ? "Không áp dụng" : recIce,
                RecommendedToppings = isFood
                    ? new List<string> { !string.IsNullOrWhiteSpace(prod.DefaultSauce) ? prod.DefaultSauce : "Tương ớt" }
                    : new List<string> { "Trân châu hoàng kim", "Kem cheese" },
                Reason = matchReason
            });
        }

        if (rankedMatches.Count == 0 && !ContainsAny(ascii, "menu", "thuc don", "co gi", "ban gi"))
        {
            replyBuilder.AppendLine($"Milky đã tìm kiếm khắp **213 món** trong thực đơn nhưng chưa thấy món nào khớp chính xác hoàn toàn với **\"{message}\"**. Dưới đây là **6 món nổi bật từ các danh mục khác nhau** để bạn tham khảo hoặc xem toàn bộ tại [Thực đơn (/menu)](/menu):\n");
        }
        else
        {
            var filterNotes = new List<string>();
            if (intent.MeaningfulKeywords.Any()) filterNotes.Add($"từ khóa `{string.Join(", ", intent.MeaningfulKeywords)}`");
            if (intent.MinPrice.HasValue && intent.MinPrice > 0 && intent.MaxPrice.HasValue)
                filterNotes.Add($"giá `{intent.MinPrice:N0}đ - {intent.MaxPrice:N0}đ`");
            else if (intent.MaxPrice.HasValue)
                filterNotes.Add($"giá `≤ {intent.MaxPrice:N0}đ`");
            if (intent.WarmOrNoIce) filterNotes.Add("`0% đá / uống ấm dịu họng`");
            if (intent.NoCaffeine) filterNotes.Add("`0% caffeine`");
            if (intent.LowSugar || intent.VeganOrNoMilk) filterNotes.Add("`ít đường / healthy`");

            var noteStr = filterNotes.Any() ? $" theo tiêu chí {string.Join(" • ", filterNotes)}" : "";
            replyBuilder.AppendLine($"✨ Milky đã tìm trong **213 món** của MilkyVerse{noteStr} và chọn ra **{rankedMatches.Count} kết quả** (hiển thị **{execContext.SuggestedDrinks.Count} món phù hợp nhất**):\n");
        }

        foreach (var s in execContext.SuggestedDrinks)
        {
            var catBadge = !string.IsNullOrWhiteSpace(s.CategoryName) ? $" *[{s.CategoryName}]*" : "";
            replyBuilder.AppendLine($"• 🧋 **{s.Name}**{catBadge} — **`{s.EstimatedPrice:N0}đ`**: {s.Description}");
        }

        replyBuilder.AppendLine("\nBạn có thể nhấn **Thêm vào giỏ** trên từng thẻ bên dưới, hoặc nhắn Milky: *\"Thêm cho tui 1 [Tên món]\"* / *\"Thay đổi độ ngọt, size\"* bất cứ lúc nào nhé!");

        return BuildFinalChatResponse(execContext, replyBuilder.ToString().Trim(), !isFallback, "MilkyVerse AI Agent Engine (213 Items Dynamic Search)", apiError);
    }

    private static string BuildSynthesizedReplyFromContext(AgentExecutionContext ctx)
    {
        var sb = new StringBuilder();
        if (ctx.CartAction != null && !string.IsNullOrWhiteSpace(ctx.CartAction.Summary))
            sb.AppendLine(ctx.CartAction.Summary);

        if (ctx.SuggestedDrinks.Any())
        {
            sb.AppendLine("Dưới đây là các gợi ý phù hợp từ thực đơn MilkyVerse:");
            foreach (var d in ctx.SuggestedDrinks)
                sb.AppendLine($"• **{d.Name}** — `{d.EstimatedPrice:N0}đ`");
        }

        if (ctx.Recipes.Any())
        {
            var r = ctx.Recipes.First();
            sb.AppendLine($"🧪 Công thức DIY **{r.RecipeName}**: {r.TeaBaseName} + {r.MilkName}, Size {r.Size}, {r.SugarLevel} đường, {r.IceLevel} đá — `{r.UnitPrice:N0}đ`.");
        }

        if (ctx.Orders.Any())
        {
            var o = ctx.Orders.First();
            sb.AppendLine($"📦 Đơn hàng **#{o.OrderCode}** đang ở trạng thái **{o.Status}** (Thanh toán: **{o.PaymentStatus}**, Tổng: `{o.TotalAmount:N0}đ`).");
        }

        return sb.Length > 0
            ? sb.ToString().Trim()
            : "Milky đã xử lý yêu cầu của bạn xong rồi nhé! Bạn cần hỗ trợ thêm món gì cứ nhắn Milky nha.";
    }

    private static AIChatResponse BuildFinalChatResponse(
        AgentExecutionContext ctx,
        string reply,
        bool isLive,
        string modelName,
        string? apiError)
    {
        return new AIChatResponse
        {
            SessionId = ctx.SessionId,
            Reply = reply,
            SuggestedDrinks = ctx.SuggestedDrinks,
            Actions = ctx.Actions,
            Recipes = ctx.Recipes,
            Customizations = ctx.Customizations,
            CartAction = ctx.CartAction,
            Orders = ctx.Orders,
            Payment = ctx.Payment,
            InventoryReport = ctx.InventoryReport,
            AdminAnalytics = ctx.AdminAnalytics,
            ExecutedTools = ctx.ExecutedTools,
            SuggestedPrompts = GetSuggestedPromptsForRole(ctx.UserRole),
            IsAiModelLive = isLive,
            ModelName = modelName,
            ApiError = apiError
        };
    }

    public static List<string> GetSuggestedPromptsForRole(string? role)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>
            {
                "📊 Phân tích doanh thu và thống kê đơn hàng",
                "🏆 Top sản phẩm bán chạy nhất hệ thống",
                "📦 Nguyên liệu nào trong kho sắp hết?",
                "👥 Thống kê khách hàng VIP mua nhiều nhất",
                "🧋 Tôi có 40.000đ, gợi ý trà sữa ít ngọt có trân châu"
            };
        }

        if (string.Equals(role, "WarehouseStaff", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>
            {
                "📦 Nguyên liệu nào sắp hết cần nhập thêm?",
                "📋 Báo cáo tổng hợp tồn kho nguyên vật liệu",
                "🔍 Món Trà sữa trân châu đen còn đủ nguyên liệu không?",
                "🧪 Tính giá ly DIY Ô long sữa tươi Size L thêm kem cheese"
            };
        }

        return new List<string>
        {
            "💸 Tôi có 40.000đ, muốn uống trà sữa ít ngọt, có trân châu",
            "🍵 Thêm cho tôi 2 ly trà sữa matcha, 1 ly ít đường và 1 ly bình thường",
            "🧪 Tự pha 1 ly Ô long sữa yến mạch Size L 30% đường thêm trân châu hoàng kim",
            "🍯 Đang bị ho / đau họng nên uống món gì ấm?",
            "🌙 Gợi ý đồ uống buổi tối không lo mất ngủ (0% caffeine)",
            "🛒 Kiểm tra giỏ hàng hiện tại của tôi",
            "📦 Theo dõi trạng thái đơn hàng gần nhất của tôi"
        };
    }

    private static bool ContainsAny(string text, params string[] phrases)
        => phrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    private static List<string> ExtractMentionedToppings(string ascii)
    {
        var list = new List<string>();
        (string Key, string Vi)[] knownToppings =
        {
            ("tran chau hoang kim", "Trân châu hoàng kim"),
            ("tran chau den", "Trân châu đen"),
            ("tran chau trang", "Trân châu trắng"),
            ("kem cheese", "Kem cheese"),
            ("pudding", "Pudding"),
            ("thach dua", "Thạch dừa"),
            ("thach nha dam", "Thạch nha đam"),
            ("flan", "Flan"),
            ("suong sao", "Sương sáo"),
            ("oreo", "Oreo")
        };

        foreach (var (key, vi) in knownToppings)
        {
            if (ascii.Contains(key)) list.Add(vi);
        }
        return list;
    }

    private static Dictionary<string, JsonElement> CreateJsonArgs(params (string Key, object Value)[] pairs)
    {
        var dict = new Dictionary<string, object>();
        foreach (var (k, v) in pairs)
        {
            dict[k] = v;
        }
        var json = JsonSerializer.Serialize(dict);
        using var doc = JsonDocument.Parse(json);
        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            result[prop.Name] = prop.Value.Clone();
        }
        return result;
    }
}
