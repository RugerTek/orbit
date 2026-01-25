using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrbitOS.Domain.Entities;

namespace OrbitOS.Api.Services;

public interface IMultiProviderAiService
{
    Task<AiProviderResponse> SendMessageAsync(
        AiAgent agent,
        string systemPrompt,
        List<ProviderMessage> messages,
        CancellationToken cancellationToken = default);
}

public class ProviderMessage
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}

public class AiProviderResponse
{
    public required string Content { get; set; }
    public int TokensUsed { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? Error { get; set; }
}

public class MultiProviderAiService : IMultiProviderAiService
{
    private readonly ILogger<MultiProviderAiService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public MultiProviderAiService(
        ILogger<MultiProviderAiService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AiProviderResponse> SendMessageAsync(
        AiAgent agent,
        string systemPrompt,
        List<ProviderMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var response = agent.Provider switch
            {
                AiProvider.Anthropic => await SendToAnthropicAsync(agent, systemPrompt, messages, cancellationToken),
                AiProvider.OpenAI => await SendToOpenAiAsync(agent, systemPrompt, messages, cancellationToken),
                AiProvider.Google => await SendToGoogleAsync(agent, systemPrompt, messages, cancellationToken),
                _ => throw new NotSupportedException($"Provider {agent.Provider} is not supported")
            };

            response.ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to {Provider}", agent.Provider);
            return new AiProviderResponse
            {
                Content = $"Error communicating with {agent.Provider}: {ex.Message}",
                Error = ex.Message,
                ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    private async Task<AiProviderResponse> SendToAnthropicAsync(
        AiAgent agent,
        string systemPrompt,
        List<ProviderMessage> messages,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["ANTHROPIC_API_KEY"]
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException("ANTHROPIC_API_KEY is not configured");

        var httpClient = _httpClientFactory.CreateClient("Anthropic");

        var requestBody = new
        {
            model = agent.ModelId,
            max_tokens = agent.MaxTokensPerResponse,
            temperature = (double)agent.Temperature,
            system = systemPrompt,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToList()
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        // Retry logic with exponential backoff for rate limits
        const int maxRetries = 3;
        var baseDelayMs = 1000;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                var content = "";
                if (root.TryGetProperty("content", out var contentArray))
                {
                    foreach (var item in contentArray.EnumerateArray())
                    {
                        if (item.TryGetProperty("type", out var typeElement) &&
                            typeElement.GetString() == "text" &&
                            item.TryGetProperty("text", out var textElement))
                        {
                            content = textElement.GetString() ?? "";
                            break;
                        }
                    }
                }

                var tokensUsed = root.TryGetProperty("usage", out var usage)
                    ? usage.GetProperty("input_tokens").GetInt32() + usage.GetProperty("output_tokens").GetInt32()
                    : 0;

                return new AiProviderResponse
                {
                    Content = content,
                    TokensUsed = tokensUsed
                };
            }

            // Handle rate limiting (429 TooManyRequests)
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt < maxRetries)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta?.TotalMilliseconds
                        ?? baseDelayMs * Math.Pow(2, attempt);

                    _logger.LogWarning(
                        "Rate limited by Anthropic API. Attempt {Attempt}/{MaxRetries}. Waiting {Delay}ms before retry.",
                        attempt + 1, maxRetries, retryAfter);

                    await Task.Delay((int)retryAfter, cancellationToken);
                    continue;
                }

                _logger.LogError("Anthropic API rate limit exceeded after {MaxRetries} retries", maxRetries);
                throw new Exception("Claude API error: TooManyRequests. Please try again later.");
            }

            _logger.LogError("Anthropic API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
            throw new Exception($"Anthropic API error: {response.StatusCode}");
        }

        throw new Exception("Anthropic API error: Maximum retries exceeded");
    }

    private async Task<AiProviderResponse> SendToOpenAiAsync(
        AiAgent agent,
        string systemPrompt,
        List<ProviderMessage> messages,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["OPENAI_API_KEY"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY is not configured");

        var httpClient = _httpClientFactory.CreateClient("OpenAI");

        var allMessages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };
        allMessages.AddRange(messages.Select(m => new { role = m.Role, content = m.Content }));

        var requestBody = new
        {
            model = agent.ModelId,
            max_tokens = agent.MaxTokensPerResponse,
            temperature = (double)agent.Temperature,
            messages = allMessages
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        // Retry logic with exponential backoff for rate limits
        const int maxRetries = 3;
        var baseDelayMs = 1000;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                var content = "";
                if (root.TryGetProperty("choices", out var choices))
                {
                    foreach (var choice in choices.EnumerateArray())
                    {
                        if (choice.TryGetProperty("message", out var message) &&
                            message.TryGetProperty("content", out var contentElement))
                        {
                            content = contentElement.GetString() ?? "";
                            break;
                        }
                    }
                }

                var tokensUsed = root.TryGetProperty("usage", out var usage) &&
                    usage.TryGetProperty("total_tokens", out var totalTokens)
                    ? totalTokens.GetInt32()
                    : 0;

                return new AiProviderResponse
                {
                    Content = content,
                    TokensUsed = tokensUsed
                };
            }

            // Handle rate limiting (429 TooManyRequests)
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt < maxRetries)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta?.TotalMilliseconds
                        ?? baseDelayMs * Math.Pow(2, attempt);

                    _logger.LogWarning(
                        "Rate limited by OpenAI API. Attempt {Attempt}/{MaxRetries}. Waiting {Delay}ms before retry.",
                        attempt + 1, maxRetries, retryAfter);

                    await Task.Delay((int)retryAfter, cancellationToken);
                    continue;
                }

                _logger.LogError("OpenAI API rate limit exceeded after {MaxRetries} retries", maxRetries);
                throw new Exception("OpenAI API error: TooManyRequests. Please try again later.");
            }

            _logger.LogError("OpenAI API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
            throw new Exception($"OpenAI API error: {response.StatusCode}");
        }

        throw new Exception("OpenAI API error: Maximum retries exceeded");
    }

    private async Task<AiProviderResponse> SendToGoogleAsync(
        AiAgent agent,
        string systemPrompt,
        List<ProviderMessage> messages,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["GOOGLE_AI_API_KEY"]
            ?? _configuration["GEMINI_API_KEY"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY")
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException("GOOGLE_AI_API_KEY or GEMINI_API_KEY is not configured");

        var httpClient = _httpClientFactory.CreateClient("Google");

        // Build contents array for Gemini
        var contents = new List<object>();

        // Add system instruction as first user message if present
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = $"System instruction: {systemPrompt}" } }
            });
            contents.Add(new
            {
                role = "model",
                parts = new[] { new { text = "Understood. I'll follow these instructions." } }
            });
        }

        // Add conversation messages
        foreach (var msg in messages)
        {
            var role = msg.Role == "assistant" ? "model" : "user";
            contents.Add(new
            {
                role,
                parts = new[] { new { text = msg.Content } }
            });
        }

        var requestBody = new
        {
            contents,
            generationConfig = new
            {
                temperature = (double)agent.Temperature,
                maxOutputTokens = agent.MaxTokensPerResponse
            }
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        // Gemini API endpoint
        var modelId = agent.ModelId.StartsWith("gemini") ? agent.ModelId : $"gemini-{agent.ModelId}";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";

        // Retry logic with exponential backoff for rate limits
        const int maxRetries = 3;
        var baseDelayMs = 1000;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                var content = "";
                if (root.TryGetProperty("candidates", out var candidates))
                {
                    foreach (var candidate in candidates.EnumerateArray())
                    {
                        if (candidate.TryGetProperty("content", out var candidateContent) &&
                            candidateContent.TryGetProperty("parts", out var parts))
                        {
                            foreach (var part in parts.EnumerateArray())
                            {
                                if (part.TryGetProperty("text", out var textElement))
                                {
                                    content = textElement.GetString() ?? "";
                                    break;
                                }
                            }
                            if (!string.IsNullOrEmpty(content)) break;
                        }
                    }
                }

                // Gemini doesn't always return token count in the same way
                var tokensUsed = 0;
                if (root.TryGetProperty("usageMetadata", out var usage) &&
                    usage.TryGetProperty("totalTokenCount", out var total))
                {
                    tokensUsed = total.GetInt32();
                }

                return new AiProviderResponse
                {
                    Content = content,
                    TokensUsed = tokensUsed
                };
            }

            // Handle rate limiting (429 TooManyRequests)
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt < maxRetries)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta?.TotalMilliseconds
                        ?? baseDelayMs * Math.Pow(2, attempt);

                    _logger.LogWarning(
                        "Rate limited by Google AI API. Attempt {Attempt}/{MaxRetries}. Waiting {Delay}ms before retry.",
                        attempt + 1, maxRetries, retryAfter);

                    await Task.Delay((int)retryAfter, cancellationToken);
                    continue;
                }

                _logger.LogError("Google AI API rate limit exceeded after {MaxRetries} retries", maxRetries);
                throw new Exception("Google AI API error: TooManyRequests. Please try again later.");
            }

            _logger.LogError("Google AI API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
            throw new Exception($"Google AI API error: {response.StatusCode}");
        }

        throw new Exception("Google AI API error: Maximum retries exceeded");
    }
}
