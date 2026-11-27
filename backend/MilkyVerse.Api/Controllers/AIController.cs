using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Services;
using MilkyVerse.Api.Services.AI;

namespace MilkyVerse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly IAIBaristaService _aiService;
    private readonly IConversationService _conversationService;
    private readonly IToolRegistry _toolRegistry;

    public AIController(
        IAIBaristaService aiService,
        IConversationService conversationService,
        IToolRegistry toolRegistry)
    {
        _aiService = aiService;
        _conversationService = conversationService;
        _toolRegistry = toolRegistry;
    }

    private (int? UserId, string Role) GetCurrentUserIdentity()
    {
        int? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var id))
        {
            userId = id;
        }

        var role = User.FindFirstValue(ClaimTypes.Role) ?? (userId.HasValue ? "Customer" : "Guest");
        return (userId, role);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var result = await _aiService.GetStatusAsync();
        return Ok(result);
    }

    [HttpGet("tools")]
    public IActionResult GetAvailableTools()
    {
        var (_, role) = GetCurrentUserIdentity();
        var tools = _toolRegistry.GetToolsForRole(role).Select(t => new
        {
            t.Name,
            t.Category,
            t.Description,
            Parameters = t.Parameters.Keys.ToList(),
            t.RequiredParameters
        });

        return Ok(ApiResponse<object>.Ok(new
        {
            role,
            toolsCount = tools.Count(),
            tools
        }));
    }

    [HttpPost("config")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Configure([FromBody] AIConfigRequest request)
    {
        var result = await _aiService.ConfigureApiKeyAsync(request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("chat")]
    [HttpPost("recommend")]
    [EnableRateLimiting("AiChatLimit")]
    public async Task<IActionResult> Chat([FromBody] AIChatRequest request)
    {
        var (userId, role) = GetCurrentUserIdentity();
        var result = await _aiService.ChatAsync(userId, request, role);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // =========================================================================
    // CONVERSATION MEMORY ENDPOINTS
    // =========================================================================
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var (userId, _) = GetCurrentUserIdentity();
        var result = await _conversationService.GetUserConversationsAsync(userId);
        return Ok(result);
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest? request)
    {
        var (userId, _) = GetCurrentUserIdentity();
        var result = await _conversationService.CreateConversationAsync(userId, request?.Title);
        return Ok(result);
    }

    [HttpGet("conversations/{sessionId:int}")]
    public async Task<IActionResult> GetConversationDetail(int sessionId)
    {
        var (userId, role) = GetCurrentUserIdentity();
        var result = await _conversationService.GetConversationDetailAsync(sessionId, userId, role);
        if (!result.Success) return ForbidOrBadRequest(result);
        return Ok(result);
    }

    [HttpDelete("conversations/{sessionId:int}")]
    public async Task<IActionResult> DeleteConversation(int sessionId)
    {
        var (userId, role) = GetCurrentUserIdentity();
        var result = await _conversationService.DeleteConversationAsync(sessionId, userId, role);
        if (!result.Success) return ForbidOrBadRequest(result);
        return Ok(result);
    }

    // =========================================================================
    // FAVORITE DIY RECIPES ENDPOINTS
    // =========================================================================
    [HttpGet("favorite-recipes")]
    public async Task<IActionResult> GetFavoriteRecipes([FromQuery] int? sessionId = null)
    {
        var (userId, _) = GetCurrentUserIdentity();
        var result = await _conversationService.GetFavoriteRecipesAsync(userId, sessionId);
        return Ok(result);
    }

    [HttpPost("favorite-recipes")]
    public async Task<IActionResult> SaveFavoriteRecipe([FromBody] FavoriteRecipeDto request, [FromQuery] int? sessionId = null)
    {
        var (userId, _) = GetCurrentUserIdentity();
        var result = await _conversationService.SaveFavoriteRecipeAsync(userId, sessionId, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("favorite-recipes/{recipeId:int}")]
    public async Task<IActionResult> DeleteFavoriteRecipe(int recipeId)
    {
        var (userId, _) = GetCurrentUserIdentity();
        var result = await _conversationService.DeleteFavoriteRecipeAsync(recipeId, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    private IActionResult ForbidOrBadRequest<T>(ApiResponse<T> res)
    {
        if (res.Message.Contains("không có quyền", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, res);
        }
        return BadRequest(res);
    }
}

public class CreateConversationRequest
{
    public string? Title { get; set; }
}
