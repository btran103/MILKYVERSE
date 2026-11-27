using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services.AI;

public interface IConversationService
{
    Task<ChatSession> GetOrCreateSessionAsync(int? sessionId, int? userId, string initialMessage);
    Task<ApiResponse<List<AIConversationSummaryDto>>> GetUserConversationsAsync(int? userId);
    Task<ApiResponse<AIConversationDetailDto>> GetConversationDetailAsync(int sessionId, int? userId, string? userRole = null);
    Task<ApiResponse<AIConversationSummaryDto>> CreateConversationAsync(int? userId, string? title = null);
    Task<ApiResponse<bool>> DeleteConversationAsync(int sessionId, int? userId, string? userRole = null);
    Task<ChatMessage> SaveUserMessageAsync(int sessionId, string content);
    Task<ChatMessage> SaveAssistantMessageAsync(int sessionId, AIChatResponse response);
    Task<ApiResponse<List<FavoriteRecipeDto>>> GetFavoriteRecipesAsync(int? userId, int? sessionId = null);
    Task<ApiResponse<FavoriteRecipeDto>> SaveFavoriteRecipeAsync(int? userId, int? sessionId, FavoriteRecipeDto request);
    Task<ApiResponse<bool>> DeleteFavoriteRecipeAsync(int recipeId, int? userId);
}

public class ConversationService : IConversationService
{
    private readonly MilkyVerseDbContext _context;

    public ConversationService(MilkyVerseDbContext context)
    {
        _context = context;
    }

    public async Task<ChatSession> GetOrCreateSessionAsync(int? sessionId, int? userId, string initialMessage)
    {
        ChatSession? session = null;

        if (sessionId.HasValue && sessionId.Value > 0)
        {
            session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId.Value);

            // Prevent reading/writing to another user's private session
            if (session != null && session.UserId.HasValue && userId.HasValue && session.UserId.Value != userId.Value)
            {
                session = null;
            }
            else if (session != null && session.UserId.HasValue && !userId.HasValue)
            {
                session = null;
            }
        }

        if (session == null)
        {
            var cleanTitle = string.IsNullOrWhiteSpace(initialMessage)
                ? "Hội thoại cùng Milky AI"
                : (initialMessage.Trim().Length > 45 ? initialMessage.Trim()[..42] + "..." : initialMessage.Trim());

            session = new ChatSession
            {
                UserId = userId,
                SessionTitle = cleanTitle,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
        }
        else
        {
            if (session.UserId == null && userId.HasValue)
            {
                session.UserId = userId;
            }
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return session;
    }

    public async Task<ApiResponse<List<AIConversationSummaryDto>>> GetUserConversationsAsync(int? userId)
    {
        if (!userId.HasValue || userId.Value <= 0)
        {
            return ApiResponse<List<AIConversationSummaryDto>>.Ok(new List<AIConversationSummaryDto>());
        }

        var sessions = await _context.ChatSessions
            .Where(s => s.UserId == userId.Value)
            .Include(s => s.Messages)
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Take(30)
            .ToListAsync();

        var dtos = sessions.Select(s =>
        {
            var lastMsg = s.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            return new AIConversationSummaryDto
            {
                Id = s.Id,
                UserId = s.UserId,
                SessionTitle = s.SessionTitle,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt ?? lastMsg?.CreatedAt ?? s.CreatedAt,
                MessageCount = s.Messages.Count,
                LastMessagePreview = lastMsg != null
                    ? (lastMsg.Content.Length > 70 ? lastMsg.Content[..67] + "..." : lastMsg.Content)
                    : string.Empty
            };
        }).ToList();

        return ApiResponse<List<AIConversationSummaryDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<AIConversationDetailDto>> GetConversationDetailAsync(int sessionId, int? userId, string? userRole = null)
    {
        var session = await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return ApiResponse<AIConversationDetailDto>.Fail("Không tìm thấy cuộc hội thoại.");
        }

        // Strict ownership check: a user cannot read another user's conversation
        if (session.UserId.HasValue)
        {
            if (!userId.HasValue || session.UserId.Value != userId.Value)
            {
                return ApiResponse<AIConversationDetailDto>.Fail("Bạn không có quyền truy cập cuộc hội thoại của người dùng khác.");
            }
        }

        var messages = session.Messages
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Select(m =>
            {
                List<SuggestedDrinkDto> drinks = new();
                if (!string.IsNullOrWhiteSpace(m.SuggestedDrinkJson))
                {
                    try
                    {
                        drinks = JsonSerializer.Deserialize<List<SuggestedDrinkDto>>(m.SuggestedDrinkJson) ?? new();
                    }
                    catch { }
                }

                AIChatResponse? payload = null;
                if (!string.IsNullOrWhiteSpace(m.AgentPayloadJson))
                {
                    try
                    {
                        payload = JsonSerializer.Deserialize<AIChatResponse>(m.AgentPayloadJson);
                    }
                    catch { }
                }

                return new AIConversationMessageDto
                {
                    Id = m.Id,
                    SenderRole = m.SenderRole,
                    Content = m.Content,
                    SuggestedDrinks = drinks,
                    AgentPayload = payload,
                    CreatedAt = m.CreatedAt
                };
            })
            .ToList();

        return ApiResponse<AIConversationDetailDto>.Ok(new AIConversationDetailDto
        {
            Id = session.Id,
            UserId = session.UserId,
            SessionTitle = session.SessionTitle,
            CreatedAt = session.CreatedAt,
            Messages = messages
        });
    }

    public async Task<ApiResponse<AIConversationSummaryDto>> CreateConversationAsync(int? userId, string? title = null)
    {
        var session = new ChatSession
        {
            UserId = userId,
            SessionTitle = string.IsNullOrWhiteSpace(title) ? "Trò chuyện mới cùng Milky" : title.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();

        return ApiResponse<AIConversationSummaryDto>.Ok(new AIConversationSummaryDto
        {
            Id = session.Id,
            UserId = session.UserId,
            SessionTitle = session.SessionTitle,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            MessageCount = 0
        });
    }

    public async Task<ApiResponse<bool>> DeleteConversationAsync(int sessionId, int? userId, string? userRole = null)
    {
        var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy cuộc hội thoại.");
        }

        if (session.UserId.HasValue && (!userId.HasValue || session.UserId.Value != userId.Value))
        {
            return ApiResponse<bool>.Fail("Bạn không có quyền xóa cuộc hội thoại của người dùng khác.");
        }

        _context.ChatSessions.Remove(session);
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã xóa cuộc hội thoại.");
    }

    public async Task<ChatMessage> SaveUserMessageAsync(int sessionId, string content)
    {
        var msg = new ChatMessage
        {
            SessionId = sessionId,
            SenderRole = "User",
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _context.ChatMessages.Add(msg);
        await _context.SaveChangesAsync();
        return msg;
    }

    public async Task<ChatMessage> SaveAssistantMessageAsync(int sessionId, AIChatResponse response)
    {
        var msg = new ChatMessage
        {
            SessionId = sessionId,
            SenderRole = "Assistant",
            Content = response.Reply,
            SuggestedDrinkJson = JsonSerializer.Serialize(response.SuggestedDrinks),
            ToolCallsJson = JsonSerializer.Serialize(response.ExecutedTools),
            AgentPayloadJson = JsonSerializer.Serialize(response),
            CreatedAt = DateTime.UtcNow
        };
        _context.ChatMessages.Add(msg);

        var session = await _context.ChatSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return msg;
    }

    public async Task<ApiResponse<List<FavoriteRecipeDto>>> GetFavoriteRecipesAsync(int? userId, int? sessionId = null)
    {
        var query = _context.FavoriteRecipes.AsQueryable();
        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(r => r.UserId == userId.Value);
        }
        else if (sessionId.HasValue && sessionId.Value > 0)
        {
            query = query.Where(r => r.SessionId == sessionId.Value && r.UserId == null);
        }
        else
        {
            return ApiResponse<List<FavoriteRecipeDto>>.Ok(new List<FavoriteRecipeDto>());
        }

        var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        var dtos = list.Select(MapFavoriteDto).ToList();
        return ApiResponse<List<FavoriteRecipeDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<FavoriteRecipeDto>> SaveFavoriteRecipeAsync(int? userId, int? sessionId, FavoriteRecipeDto request)
    {
        var entity = new FavoriteRecipe
        {
            UserId = userId,
            SessionId = sessionId,
            RecipeName = string.IsNullOrWhiteSpace(request.RecipeName) ? "Công thức DIY Yêu Thích" : request.RecipeName.Trim(),
            TeaBaseId = string.IsNullOrWhiteSpace(request.TeaBaseId) ? "black_tea" : request.TeaBaseId,
            TeaBaseName = request.TeaBaseName,
            MilkId = string.IsNullOrWhiteSpace(request.MilkId) ? "fresh_milk" : request.MilkId,
            MilkName = request.MilkName,
            Size = string.IsNullOrWhiteSpace(request.Size) ? "M" : request.Size,
            SugarLevel = string.IsNullOrWhiteSpace(request.SugarLevel) ? "50%" : request.SugarLevel,
            IceLevel = string.IsNullOrWhiteSpace(request.IceLevel) ? "70%" : request.IceLevel,
            ToppingIdsJson = JsonSerializer.Serialize(request.ToppingIds),
            ToppingNamesJson = JsonSerializer.Serialize(request.ToppingNames),
            UnitPrice = request.UnitPrice,
            Notes = request.Notes ?? string.Empty,
            IsReferenceFormula = request.IsReferenceFormula,
            CreatedAt = DateTime.UtcNow
        };

        _context.FavoriteRecipes.Add(entity);
        await _context.SaveChangesAsync();

        return ApiResponse<FavoriteRecipeDto>.Ok(MapFavoriteDto(entity), "Đã lưu công thức yêu thích thành công!");
    }

    public async Task<ApiResponse<bool>> DeleteFavoriteRecipeAsync(int recipeId, int? userId)
    {
        var entity = await _context.FavoriteRecipes.FindAsync(recipeId);
        if (entity == null) return ApiResponse<bool>.Fail("Không tìm thấy công thức.");
        if (entity.UserId.HasValue && (!userId.HasValue || entity.UserId.Value != userId.Value))
        {
            return ApiResponse<bool>.Fail("Bạn không có quyền xóa công thức của người khác.");
        }

        _context.FavoriteRecipes.Remove(entity);
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã xóa công thức yêu thích.");
    }

    private static FavoriteRecipeDto MapFavoriteDto(FavoriteRecipe r)
    {
        List<int> ids = new();
        List<string> names = new();
        try { ids = JsonSerializer.Deserialize<List<int>>(r.ToppingIdsJson) ?? new(); } catch { }
        try { names = JsonSerializer.Deserialize<List<string>>(r.ToppingNamesJson) ?? new(); } catch { }

        return new FavoriteRecipeDto
        {
            Id = r.Id,
            UserId = r.UserId,
            RecipeName = r.RecipeName,
            TeaBaseId = r.TeaBaseId,
            TeaBaseName = r.TeaBaseName,
            MilkId = r.MilkId,
            MilkName = r.MilkName,
            Size = r.Size,
            SugarLevel = r.SugarLevel,
            IceLevel = r.IceLevel,
            ToppingIds = ids,
            ToppingNames = names,
            UnitPrice = r.UnitPrice,
            Notes = r.Notes,
            IsReferenceFormula = r.IsReferenceFormula,
            CreatedAt = r.CreatedAt
        };
    }
}
