using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Services;

namespace MilkyVerse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly IAIBaristaService _aiService;

    public AIController(IAIBaristaService aiService)
    {
        _aiService = aiService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var result = await _aiService.GetStatusAsync();
        return Ok(result);
    }

    [HttpPost("config")]
    public async Task<IActionResult> Configure([FromBody] AIConfigRequest request)
    {
        var result = await _aiService.ConfigureApiKeyAsync(request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("chat")]
    [HttpPost("recommend")]
    public async Task<IActionResult> Chat([FromBody] AIChatRequest request)
    {
        int? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var id))
        {
            userId = id;
        }

        var result = await _aiService.ChatAsync(userId, request);
        return Ok(result);
    }
}
