using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Services.AI;

namespace MilkyVerse.Api.Services;

public interface IAIBaristaService
{
    Task<ApiResponse<AIChatResponse>> ChatAsync(int? userId, AIChatRequest request, string? userRole = null);
    Task<ApiResponse<AIStatusResponse>> GetStatusAsync();
    Task<ApiResponse<AIStatusResponse>> ConfigureApiKeyAsync(AIConfigRequest request);
}

public class AIBaristaService : IAIBaristaService
{
    private const string DefaultModel = "gemini-2.5-flash";

    private readonly MilkyVerseDbContext _context;
    private readonly IConfiguration _config;
    private readonly IMilkyAIOrchestrator _orchestrator;
    private readonly IToolRegistry _toolRegistry;

    public AIBaristaService(
        MilkyVerseDbContext context,
        IConfiguration config,
        IMilkyAIOrchestrator orchestrator,
        IToolRegistry toolRegistry)
    {
        _context = context;
        _config = config;
        _orchestrator = orchestrator;
        _toolRegistry = toolRegistry;
    }

    private string? ResolveServerApiKey()
    {
        // Strictly read from environment variable GEMINI_API_KEY or server configuration (never from client request or shared static field)
        var envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey) &&
            envKey != "YOUR_GEMINI_API_KEY_HERE" &&
            envKey != "your_gemini_or_groq_or_openai_api_key")
        {
            return envKey.Trim();
        }

        var cfgKey = _config["Gemini:ApiKey"];
        if (!string.IsNullOrWhiteSpace(cfgKey) &&
            cfgKey != "YOUR_GEMINI_API_KEY_HERE" &&
            cfgKey != "your_gemini_or_groq_or_openai_api_key")
        {
            return cfgKey.Trim();
        }

        return null;
    }

    private static string DetectProvider(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return "MilkyVerse Local Agent Engine";
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
        var activeKey = ResolveServerApiKey();
        var provider = DetectProvider(activeKey);
        var menuCount = await _context.Products.CountAsync(p => p.IsActive);
        var toppingCount = await _context.Toppings.CountAsync(t => t.IsAvailable);
        var toolsCount = _toolRegistry.GetAllTools().Count;

        return ApiResponse<AIStatusResponse>.Ok(new AIStatusResponse
        {
            IsConfigured = !string.IsNullOrWhiteSpace(activeKey),
            Provider = !string.IsNullOrWhiteSpace(activeKey)
                ? provider
                : "MilkyVerse AI Agent Orchestrator (Local Tool-Calling Engine)",
            ActiveModel = !string.IsNullOrWhiteSpace(activeKey) ? DefaultModel : "milky-agent-v3-tools",
            MaskedApiKey = MaskKey(activeKey),
            EndpointUrl = "/api/ai/chat",
            ActiveMenuCount = menuCount,
            ActiveToppingCount = toppingCount,
            AvailableToolsCount = toolsCount
        });
    }

    public async Task<ApiResponse<AIStatusResponse>> ConfigureApiKeyAsync(AIConfigRequest request)
    {
        var cleanKey = request.ApiKey?.Trim() ?? string.Empty;
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", string.IsNullOrWhiteSpace(cleanKey) ? null : cleanKey);

        try
        {
            var localSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");
            Dictionary<string, object> dict = new();
            if (File.Exists(localSettingsPath))
            {
                var raw = await File.ReadAllTextAsync(localSettingsPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, object>>(raw) ?? new();
            }
            dict["Gemini"] = new { ApiKey = cleanKey };
            var updated = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(localSettingsPath, updated);
        }
        catch
        {
            // Ignore in read-only environments
        }

        return await GetStatusAsync();
    }

    public async Task<ApiResponse<AIChatResponse>> ChatAsync(int? userId, AIChatRequest request, string? userRole = null)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return ApiResponse<AIChatResponse>.Fail("Nội dung tin nhắn không được để trống.");
        }

        var activeKey = ResolveServerApiKey();
        var provider = DetectProvider(activeKey);

        var response = await _orchestrator.ProcessChatAsync(
            userId,
            userRole ?? "Guest",
            request,
            activeKey,
            provider,
            DefaultModel);

        return ApiResponse<AIChatResponse>.Ok(response);
    }
}
