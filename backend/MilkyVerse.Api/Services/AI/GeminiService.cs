using System.Text;
using System.Text.Json;

namespace MilkyVerse.Api.Services.AI;

public class LlmFunctionCall
{
    public string CallId { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Arguments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string RawArgumentsJson { get; set; } = "{}";
}

public class LlmToolResponseItem
{
    public string CallId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = "{}";
    public object ResponseData { get; set; } = new { };
}

public class LlmChatHistoryTurn
{
    public string Role { get; set; } = "user"; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
}

public class LlmTurnRequest
{
    public string ApiKey { get; set; } = string.Empty;
    public string Provider { get; set; } = "Google Gemini API";
    public string Model { get; set; } = "gemini-2.5-flash";
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public List<LlmChatHistoryTurn> History { get; set; } = new();
    public List<LlmToolResponseItem> PreviousToolExecutions { get; set; } = new();
    public string UserRole { get; set; } = "Customer";
}

public class LlmTurnResponse
{
    public bool Success { get; set; } = true;
    public string TextReply { get; set; } = string.Empty;
    public List<LlmFunctionCall> FunctionCalls { get; set; } = new();
    public string UsedModel { get; set; } = "gemini-2.5-flash";
    public string? ErrorMessage { get; set; }
    public bool HasFunctionCalls => FunctionCalls.Count > 0;
}

public interface IGeminiService
{
    Task<LlmTurnResponse> GenerateTurnAsync(LlmTurnRequest request, CancellationToken cancellationToken = default);
}

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(IHttpClientFactory httpClientFactory, IToolRegistry toolRegistry, ILogger<GeminiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(25);
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public async Task<LlmTurnResponse> GenerateTurnAsync(LlmTurnRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return new LlmTurnResponse
            {
                Success = false,
                ErrorMessage = "Chưa cấu hình API Key cho dịch vụ AI."
            };
        }

        if (request.Provider.Contains("Groq", StringComparison.OrdinalIgnoreCase) ||
            request.Provider.Contains("OpenAI", StringComparison.OrdinalIgnoreCase) ||
            request.Provider.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            return await CallOpenAiCompatibleWithToolsAsync(request, cancellationToken);
        }

        return await CallGeminiWithFunctionCallingAsync(request, cancellationToken);
    }

    private async Task<LlmTurnResponse> CallGeminiWithFunctionCallingAsync(LlmTurnRequest request, CancellationToken cancellationToken)
    {
        var candidateModels = new[] { "gemini-2.5-flash", "gemini-2.0-flash", "gemini-2.5-flash-lite" };
        Exception? lastEx = null;

        var contents = new List<object>();

        // 1. Conversation history
        foreach (var h in request.History)
        {
            contents.Add(new
            {
                role = string.Equals(h.Role, "user", StringComparison.OrdinalIgnoreCase) ? "user" : "model",
                parts = new[] { new { text = h.Content } }
            });
        }

        // 2. Current user message
        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = request.UserMessage } }
        });

        // 3. Intermediate Function Calls and Function Responses in the current agent loop
        foreach (var exec in request.PreviousToolExecutions)
        {
            object parsedArgs;
            try
            {
                parsedArgs = JsonSerializer.Deserialize<JsonElement>(exec.ArgumentsJson);
            }
            catch
            {
                parsedArgs = new { };
            }

            contents.Add(new
            {
                role = "model",
                parts = new[]
                {
                    new
                    {
                        functionCall = new
                        {
                            name = exec.Name,
                            args = parsedArgs
                        }
                    }
                }
            });

            contents.Add(new
            {
                role = "function",
                parts = new[]
                {
                    new
                    {
                        functionResponse = new
                        {
                            name = exec.Name,
                            response = new
                            {
                                name = exec.Name,
                                content = exec.ResponseData
                            }
                        }
                    }
                }
            });
        }

        var toolsPayload = _toolRegistry.BuildGeminiToolsDeclaration(request.UserRole);

        foreach (var modelName in candidateModels)
        {
            try
            {
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={request.ApiKey}";
                var payload = new
                {
                    systemInstruction = new
                    {
                        parts = new[] { new { text = request.SystemPrompt } }
                    },
                    contents = contents,
                    tools = toolsPayload,
                    generationConfig = new
                    {
                        temperature = 0.35,
                        maxOutputTokens = 1500
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                using var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var httpRes = await _httpClient.PostAsync(endpoint, httpContent, cancellationToken);

                if (!httpRes.IsSuccessStatusCode)
                {
                    var errBody = await httpRes.Content.ReadAsStringAsync(cancellationToken);
                    var statusCode = (int)httpRes.StatusCode;
                    // Fail fast on 400/401/403 (e.g. API_KEY_INVALID) instead of retrying 3 models
                    if (statusCode == 400 || statusCode == 401 || statusCode == 403)
                    {
                        return new LlmTurnResponse
                        {
                            Success = false,
                            ErrorMessage = $"Gemini ({modelName}) HTTP {statusCode}: {errBody}"
                        };
                    }
                    throw new Exception($"Gemini ({modelName}) HTTP {statusCode}: {errBody}");
                }

                var rawJson = await httpRes.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(rawJson);

                var result = new LlmTurnResponse
                {
                    Success = true,
                    UsedModel = modelName
                };

                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                        contentElem.TryGetProperty("parts", out var partsElem))
                    {
                        var textBuilder = new StringBuilder();
                        foreach (var part in partsElem.EnumerateArray())
                        {
                            if (part.TryGetProperty("functionCall", out var fnCall))
                            {
                                var fnName = fnCall.GetProperty("name").GetString() ?? "";
                                var argsDict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                                string rawArgs = "{}";

                                if (fnCall.TryGetProperty("args", out var argsElem) && argsElem.ValueKind == JsonValueKind.Object)
                                {
                                    rawArgs = argsElem.GetRawText();
                                    foreach (var prop in argsElem.EnumerateObject())
                                    {
                                        argsDict[prop.Name] = prop.Value.Clone();
                                    }
                                }

                                result.FunctionCalls.Add(new LlmFunctionCall
                                {
                                    Name = fnName,
                                    Arguments = argsDict,
                                    RawArgumentsJson = rawArgs
                                });
                            }
                            else if (part.TryGetProperty("text", out var textElem))
                            {
                                textBuilder.Append(textElem.GetString());
                            }
                        }

                        result.TextReply = textBuilder.ToString().Trim();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini model {Model} call failed, trying next fallback if available.", modelName);
                lastEx = ex;
            }
        }

        return new LlmTurnResponse
        {
            Success = false,
            ErrorMessage = lastEx?.Message ?? "Không thể kết nối tới Google Gemini API."
        };
    }

    private async Task<LlmTurnResponse> CallOpenAiCompatibleWithToolsAsync(LlmTurnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = request.Provider.Contains("Groq", StringComparison.OrdinalIgnoreCase)
                ? "https://api.groq.com/openai/v1/chat/completions"
                : request.Provider.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase)
                    ? "https://openrouter.ai/api/v1/chat/completions"
                    : "https://api.openai.com/v1/chat/completions";

            var usedModel = request.Provider.Contains("Groq", StringComparison.OrdinalIgnoreCase)
                ? "llama-3.3-70b-versatile"
                : request.Provider.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase)
                    ? "google/gemini-2.5-flash"
                    : "gpt-4o-mini";

            var messagesList = new List<object>
            {
                new { role = "system", content = request.SystemPrompt }
            };

            foreach (var h in request.History)
            {
                messagesList.Add(new
                {
                    role = string.Equals(h.Role, "user", StringComparison.OrdinalIgnoreCase) ? "user" : "assistant",
                    content = h.Content
                });
            }

            messagesList.Add(new { role = "user", content = request.UserMessage });

            foreach (var exec in request.PreviousToolExecutions)
            {
                var callId = string.IsNullOrWhiteSpace(exec.CallId) ? "call_" + exec.Name : exec.CallId;
                messagesList.Add(new
                {
                    role = "assistant",
                    content = (string?)null,
                    tool_calls = new[]
                    {
                        new
                        {
                            id = callId,
                            type = "function",
                            function = new
                            {
                                name = exec.Name,
                                arguments = exec.ArgumentsJson
                            }
                        }
                    }
                });

                messagesList.Add(new
                {
                    role = "tool",
                    tool_call_id = callId,
                    name = exec.Name,
                    content = JsonSerializer.Serialize(exec.ResponseData)
                });
            }

            var toolsPayload = _toolRegistry.BuildOpenAiToolsDeclaration(request.UserRole);

            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, endpoint);
            reqMsg.Headers.Add("Authorization", $"Bearer {request.ApiKey}");
            reqMsg.Content = new StringContent(JsonSerializer.Serialize(new
            {
                model = usedModel,
                messages = messagesList,
                tools = toolsPayload,
                tool_choice = "auto",
                temperature = 0.35
            }), Encoding.UTF8, "application/json");

            var httpRes = await _httpClient.SendAsync(reqMsg, cancellationToken);
            if (!httpRes.IsSuccessStatusCode)
            {
                var errBody = await httpRes.Content.ReadAsStringAsync(cancellationToken);
                return new LlmTurnResponse
                {
                    Success = false,
                    ErrorMessage = $"{request.Provider} HTTP {(int)httpRes.StatusCode}: {errBody}"
                };
            }

            var json = await httpRes.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var messageElem = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

            var response = new LlmTurnResponse
            {
                Success = true,
                UsedModel = usedModel
            };

            if (messageElem.TryGetProperty("content", out var contentElem) && contentElem.ValueKind == JsonValueKind.String)
            {
                response.TextReply = contentElem.GetString() ?? string.Empty;
            }

            if (messageElem.TryGetProperty("tool_calls", out var toolCallsElem) && toolCallsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var tc in toolCallsElem.EnumerateArray())
                {
                    var callId = tc.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N");
                    var fn = tc.GetProperty("function");
                    var fnName = fn.GetProperty("name").GetString() ?? "";
                    var rawArgs = fn.TryGetProperty("arguments", out var argEl) ? argEl.GetString() ?? "{}" : "{}";

                    var argsDict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                    try
                    {
                        using var argsDoc = JsonDocument.Parse(rawArgs);
                        if (argsDoc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in argsDoc.RootElement.EnumerateObject())
                            {
                                argsDict[prop.Name] = prop.Value.Clone();
                            }
                        }
                    }
                    catch { }

                    response.FunctionCalls.Add(new LlmFunctionCall
                    {
                        CallId = callId,
                        Name = fnName,
                        Arguments = argsDict,
                        RawArgumentsJson = rawArgs
                    });
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            return new LlmTurnResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
