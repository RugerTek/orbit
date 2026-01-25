using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Services;

public interface IAiChatService
{
    Task<AiChatResponse> ChatAsync(Guid organizationId, AiChatRequest request, CancellationToken cancellationToken = default);
}

public class AiChatRequest
{
    public required string Message { get; set; }
    public List<AiChatMessage>? History { get; set; }
    public string? Context { get; set; }
}

public class AiChatMessage
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}

public class AiChatResponse
{
    public required string Message { get; set; }
    public List<AiToolCall>? ToolCalls { get; set; }
    public AiChatError? Error { get; set; }
}

public class AiToolCall
{
    public required string Tool { get; set; }
    public required string Action { get; set; }
    public JsonElement? Data { get; set; }
}

public class AiChatError
{
    public required string Code { get; set; }
    public required string Message { get; set; }
}

public class AiChatService : IAiChatService
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly IOrganizationContextService _contextService;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<AiChatService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ANTHROPIC_API_URL = "https://api.anthropic.com/v1/messages";

    public AiChatService(
        OrbitOSDbContext dbContext,
        IOrganizationContextService contextService,
        IKnowledgeBaseService knowledgeBaseService,
        ILogger<AiChatService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _contextService = contextService;
        _knowledgeBaseService = knowledgeBaseService;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Anthropic");
        _apiKey = configuration["ANTHROPIC_API_KEY"]
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException("ANTHROPIC_API_KEY is not configured");
    }

    public async Task<AiChatResponse> ChatAsync(Guid organizationId, AiChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use shared context service
            var orgContext = await _contextService.BuildContextAsync(organizationId, cancellationToken);
            var systemPrompt = _contextService.BuildSystemPrompt(orgContext);
            var messages = new List<ClaudeMessage>();

            if (request.History != null)
            {
                foreach (var msg in request.History)
                {
                    messages.Add(new ClaudeMessage { Role = msg.Role, Content = msg.Content });
                }
            }

            messages.Add(new ClaudeMessage { Role = "user", Content = request.Message });

            var tools = BuildTools();
            var response = await CallClaudeApiAsync(systemPrompt, messages, tools, cancellationToken);

            return await ProcessResponseAsync(organizationId, response, messages, tools, systemPrompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI chat for organization {OrganizationId}", organizationId);
            return new AiChatResponse
            {
                Message = "I'm sorry, I encountered an error processing your request. Please try again.",
                Error = new AiChatError { Code = "CHAT_ERROR", Message = ex.Message }
            };
        }
    }

    private async Task<ClaudeResponse> CallClaudeApiAsync(
        string systemPrompt,
        List<ClaudeMessage> messages,
        List<ClaudeTool> tools,
        CancellationToken cancellationToken)
    {
        var requestBody = new ClaudeRequest
        {
            Model = "claude-sonnet-4-20250514",
            MaxTokens = 8192, // Doubled from 4096 to allow longer responses
            System = systemPrompt,
            Messages = messages,
            Tools = tools.Count > 0 ? tools : null
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        // Retry logic with exponential backoff for rate limits
        // Rate limit is 10,000 tokens/minute, so we need longer delays
        const int maxRetries = 3;
        var baseDelayMs = 15000; // 15 seconds base delay for rate limits

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ANTHROPIC_API_URL);
            httpRequest.Headers.Add("x-api-key", _apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ClaudeResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }) ?? throw new Exception("Failed to parse Claude response");
            }

            // Handle rate limiting (429 TooManyRequests)
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt < maxRetries)
                {
                    // Check for Retry-After header, otherwise use exponential backoff
                    // Delays: 15s, 30s, 60s
                    var retryAfter = httpResponse.Headers.RetryAfter?.Delta?.TotalMilliseconds
                        ?? baseDelayMs * Math.Pow(2, attempt);

                    _logger.LogWarning(
                        "Rate limited by Claude API. Attempt {Attempt}/{MaxRetries}. Waiting {Delay}ms before retry.",
                        attempt + 1, maxRetries, retryAfter);

                    await Task.Delay((int)retryAfter, cancellationToken);
                    continue;
                }

                _logger.LogError("Claude API rate limit exceeded after {MaxRetries} retries", maxRetries);
                throw new Exception("I'm receiving too many requests right now. Please wait about 30 seconds and try again.");
            }

            // For other errors, log and throw immediately
            _logger.LogError("Claude API error: {StatusCode} - {Response}", httpResponse.StatusCode, responseJson);
            throw new Exception($"Claude API error: {httpResponse.StatusCode}");
        }

        throw new Exception("Claude API error: Maximum retries exceeded");
    }

    private List<ClaudeTool> BuildTools()
    {
        return new List<ClaudeTool>
        {
            new ClaudeTool
            {
                Name = "create_person",
                Description = "Create a new person in the organization",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The person's full name" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "Optional description or job title" },
                        ["resourceSubtypeId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the person subtype" }
                    },
                    Required = new[] { "name", "resourceSubtypeId" }
                }
            },
            new ClaudeTool
            {
                Name = "update_person",
                Description = "Update an existing person's information",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["personId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the person to update" },
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The person's new name" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "The person's new description" },
                        ["status"] = new ClaudeToolProperty { Type = "string", Description = "The person's status (Active, Inactive, Archived)" }
                    },
                    Required = new[] { "personId" }
                }
            },
            new ClaudeTool
            {
                Name = "assign_role",
                Description = "Assign a role to a person",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["personId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the person" },
                        ["roleId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the role to assign" },
                        ["allocationPercentage"] = new ClaudeToolProperty { Type = "number", Description = "Percentage of time allocated (0-100)" },
                        ["isPrimary"] = new ClaudeToolProperty { Type = "boolean", Description = "Whether this is the primary role" }
                    },
                    Required = new[] { "personId", "roleId" }
                }
            },
            new ClaudeTool
            {
                Name = "add_capability",
                Description = "Add a function capability to a person",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["personId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the person" },
                        ["functionId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the function" },
                        ["level"] = new ClaudeToolProperty { Type = "string", Description = "Capability level: Learning, Capable, Expert, Master" }
                    },
                    Required = new[] { "personId", "functionId" }
                }
            },
            new ClaudeTool
            {
                Name = "analyze_coverage",
                Description = "Analyze organizational coverage to find gaps and single points of failure",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>(),
                    Required = Array.Empty<string>()
                }
            },
            // Function Management Tools
            new ClaudeTool
            {
                Name = "create_function",
                Description = "Create a new business function (capability/skill) in the organization",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The function name (e.g., 'Data Migration', 'Requirements Gathering')" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "Detailed description of what this function does" },
                        ["purpose"] = new ClaudeToolProperty { Type = "string", Description = "The business purpose or goal this function serves" },
                        ["category"] = new ClaudeToolProperty { Type = "string", Description = "Category to group related functions (e.g., 'Technical', 'Sales', 'Support')" }
                    },
                    Required = new[] { "name" }
                }
            },
            new ClaudeTool
            {
                Name = "update_function",
                Description = "Update an existing business function's details",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["functionId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the function to update" },
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The new name for the function" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "The new description" },
                        ["purpose"] = new ClaudeToolProperty { Type = "string", Description = "The new purpose" },
                        ["category"] = new ClaudeToolProperty { Type = "string", Description = "The new category" }
                    },
                    Required = new[] { "functionId" }
                }
            },
            new ClaudeTool
            {
                Name = "bulk_create_functions",
                Description = "Create multiple business functions at once. Use this when the user wants to add several related functions.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["functions"] = new ClaudeToolProperty { Type = "array", Description = "Array of function objects, each with name, description (optional), purpose (optional), and category (optional)" }
                    },
                    Required = new[] { "functions" }
                }
            },
            new ClaudeTool
            {
                Name = "suggest_functions",
                Description = "Suggest new business functions based on the organization's existing data, industry, or specific context provided by the user",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["context"] = new ClaudeToolProperty { Type = "string", Description = "Optional context about what area to focus suggestions on (e.g., 'consulting', 'support', 'technical')" },
                        ["count"] = new ClaudeToolProperty { Type = "number", Description = "Number of suggestions to generate (default: 5)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "suggest_improvements",
                Description = "Analyze existing functions and suggest improvements such as better categorization, missing descriptions, or function consolidation opportunities",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>(),
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "delete_function",
                Description = "Delete a business function from the organization",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["functionId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the function to delete" }
                    },
                    Required = new[] { "functionId" }
                }
            },
            // Process Management Tools
            new ClaudeTool
            {
                Name = "create_process",
                Description = "Create a new business process",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The process name" },
                        ["purpose"] = new ClaudeToolProperty { Type = "string", Description = "The purpose of this process" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "Detailed description" },
                        ["trigger"] = new ClaudeToolProperty { Type = "string", Description = "What triggers this process" },
                        ["output"] = new ClaudeToolProperty { Type = "string", Description = "Expected output" },
                        ["frequency"] = new ClaudeToolProperty { Type = "string", Description = "Frequency: Daily, Weekly, Monthly, OnDemand, Continuous" }
                    },
                    Required = new[] { "name" }
                }
            },
            new ClaudeTool
            {
                Name = "bulk_create_processes",
                Description = "Create multiple business processes at once",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["processes"] = new ClaudeToolProperty { Type = "array", Description = "Array of process objects with name, purpose, description, trigger, output, frequency" }
                    },
                    Required = new[] { "processes" }
                }
            },
            new ClaudeTool
            {
                Name = "add_activities_to_process",
                Description = "Add activities (steps) to an existing process. Use IE symbols: Operation (circle) for value-adding work, Inspection (square) for quality checks, Transport (arrow) for movement, Delay (D) for waiting, Storage (triangle) for storage.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["processId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the process to add activities to" },
                        ["activities"] = new ClaudeToolProperty { Type = "array", Description = "Array of activity objects. Each activity should have: name (string), description (string), activityType (Manual, Automated, Hybrid, Decision, Handoff - use Manual for most IE operations), instructions (optional string), estimatedDurationMinutes (optional number), order (optional number for sequence)" }
                    },
                    Required = new[] { "processId", "activities" }
                }
            },
            // Goal Management Tools
            new ClaudeTool
            {
                Name = "create_goal",
                Description = "Create a new goal, objective, or key result",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The goal name" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "Description of the goal" },
                        ["goalType"] = new ClaudeToolProperty { Type = "string", Description = "Type: Objective, KeyResult, Initiative" },
                        ["targetValue"] = new ClaudeToolProperty { Type = "number", Description = "Target value to achieve" },
                        ["unit"] = new ClaudeToolProperty { Type = "string", Description = "Unit (%, $, count, etc.)" },
                        ["parentId"] = new ClaudeToolProperty { Type = "string", Description = "Parent goal ID for hierarchy" }
                    },
                    Required = new[] { "name", "goalType" }
                }
            },
            new ClaudeTool
            {
                Name = "bulk_create_goals",
                Description = "Create multiple goals at once (useful for OKR sets)",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["goals"] = new ClaudeToolProperty { Type = "array", Description = "Array of goal objects with name, description, goalType, targetValue, unit" }
                    },
                    Required = new[] { "goals" }
                }
            },
            // Partner Management Tools
            new ClaudeTool
            {
                Name = "create_partner",
                Description = "Create a new partner (supplier, distributor, strategic partner)",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The partner name" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "Description of the partnership" },
                        ["type"] = new ClaudeToolProperty { Type = "string", Description = "Type: Supplier, Distributor, Strategic, Technology, Agency, Reseller, Affiliate, JointVenture" },
                        ["strategicValue"] = new ClaudeToolProperty { Type = "string", Description = "Value: Critical, High, Medium, Low" }
                    },
                    Required = new[] { "name", "type" }
                }
            },
            new ClaudeTool
            {
                Name = "bulk_create_partners",
                Description = "Create multiple partners at once",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["partners"] = new ClaudeToolProperty { Type = "array", Description = "Array of partner objects with name, description, type, strategicValue" }
                    },
                    Required = new[] { "partners" }
                }
            },
            // Channel Management Tools
            new ClaudeTool
            {
                Name = "create_channel",
                Description = "Create a new channel (sales, marketing, distribution)",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The channel name" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "Description" },
                        ["type"] = new ClaudeToolProperty { Type = "string", Description = "Type: Direct, Indirect, Digital, Physical, Hybrid" },
                        ["category"] = new ClaudeToolProperty { Type = "string", Description = "Category: Sales, Marketing, Distribution, Support, Communication" },
                        ["ownership"] = new ClaudeToolProperty { Type = "string", Description = "Ownership: Owned, Partner, ThirdParty" }
                    },
                    Required = new[] { "name", "type", "category" }
                }
            },
            new ClaudeTool
            {
                Name = "bulk_create_channels",
                Description = "Create multiple channels at once",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["channels"] = new ClaudeToolProperty { Type = "array", Description = "Array of channel objects with name, description, type, category, ownership" }
                    },
                    Required = new[] { "channels" }
                }
            },
            // Value Proposition Management Tools
            new ClaudeTool
            {
                Name = "create_value_proposition",
                Description = "Create a new value proposition",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The value proposition name" },
                        ["headline"] = new ClaudeToolProperty { Type = "string", Description = "Short headline" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "Detailed description" }
                    },
                    Required = new[] { "name", "headline" }
                }
            },
            new ClaudeTool
            {
                Name = "bulk_create_value_propositions",
                Description = "Create multiple value propositions at once",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["valuePropositions"] = new ClaudeToolProperty { Type = "array", Description = "Array of value proposition objects with name, headline, description" }
                    },
                    Required = new[] { "valuePropositions" }
                }
            },
            // Customer Relationship Management Tools
            new ClaudeTool
            {
                Name = "create_customer_relationship",
                Description = "Create a new customer relationship type",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The relationship name" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "Description" },
                        ["type"] = new ClaudeToolProperty { Type = "string", Description = "Type: PersonalAssistance, DedicatedAssistance, SelfService, AutomatedService, Communities, CoCreation" }
                    },
                    Required = new[] { "name", "type" }
                }
            },
            new ClaudeTool
            {
                Name = "bulk_create_customer_relationships",
                Description = "Create multiple customer relationship types at once",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["customerRelationships"] = new ClaudeToolProperty { Type = "array", Description = "Array of customer relationship objects with name, description, type" }
                    },
                    Required = new[] { "customerRelationships" }
                }
            },
            // Revenue Stream Management Tools
            new ClaudeTool
            {
                Name = "create_revenue_stream",
                Description = "Create a new revenue stream",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The revenue stream name" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "Description" },
                        ["type"] = new ClaudeToolProperty { Type = "string", Description = "Type: AssetSale, UsageFee, Subscription, Licensing, Brokerage, Advertising, Leasing, Commission" },
                        ["pricingMechanism"] = new ClaudeToolProperty { Type = "string", Description = "Pricing: Fixed, Dynamic, Negotiated, Auction, MarketDependent, VolumeDependent" }
                    },
                    Required = new[] { "name", "type" }
                }
            },
            new ClaudeTool
            {
                Name = "bulk_create_revenue_streams",
                Description = "Create multiple revenue streams at once",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["revenueStreams"] = new ClaudeToolProperty { Type = "array", Description = "Array of revenue stream objects with name, description, type, pricingMechanism" }
                    },
                    Required = new[] { "revenueStreams" }
                }
            },
            // Role Management Tool
            new ClaudeTool
            {
                Name = "create_role",
                Description = "Create a new organizational role",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The role name" },
                        ["description"] = new ClaudeToolProperty { Type = "string", Description = "Description of responsibilities" },
                        ["department"] = new ClaudeToolProperty { Type = "string", Description = "Department this role belongs to" }
                    },
                    Required = new[] { "name" }
                }
            },
            new ClaudeTool
            {
                Name = "bulk_create_roles",
                Description = "Create multiple roles at once",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["roles"] = new ClaudeToolProperty { Type = "array", Description = "Array of role objects with name, description, department" }
                    },
                    Required = new[] { "roles" }
                }
            },
            // ===== AI Agent Management Tools =====
            new ClaudeTool
            {
                Name = "create_ai_agent",
                Description = "Create a new AI agent with full configuration including personality, model settings, and behavior",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The agent's display name (e.g., 'Chief Financial Officer')" },
                        ["roleTitle"] = new ClaudeToolProperty { Type = "string", Description = "The agent's role/title in the organization (e.g., 'CFO', 'Head of Operations')" },
                        ["systemPrompt"] = new ClaudeToolProperty { Type = "string", Description = "The system prompt that defines the agent's personality, expertise, and behavior" },
                        ["provider"] = new ClaudeToolProperty { Type = "string", Description = "AI provider: Anthropic, OpenAI, or Google (default: Anthropic)" },
                        ["modelId"] = new ClaudeToolProperty { Type = "string", Description = "Model ID (e.g., 'claude-sonnet-4-20250514', 'gpt-4o'). Default: claude-sonnet-4-20250514" },
                        ["modelDisplayName"] = new ClaudeToolProperty { Type = "string", Description = "Display name for the model (e.g., 'Claude Sonnet'). Default: Claude Sonnet" },
                        ["avatarColor"] = new ClaudeToolProperty { Type = "string", Description = "Hex color for avatar (e.g., '#4F46E5')" },
                        ["maxTokensPerResponse"] = new ClaudeToolProperty { Type = "integer", Description = "Max tokens per response (default: 4096)" },
                        ["temperature"] = new ClaudeToolProperty { Type = "number", Description = "Temperature setting 0.0-2.0 (default: 0.7)" },
                        ["assertiveness"] = new ClaudeToolProperty { Type = "integer", Description = "How likely to speak up 0-100 (default: 50). High: speaks first. Low: waits to be asked" },
                        ["communicationStyle"] = new ClaudeToolProperty { Type = "string", Description = "Style: Formal, Casual, Direct, Diplomatic, Analytical (default: Formal)" },
                        ["reactionTendency"] = new ClaudeToolProperty { Type = "string", Description = "Reaction: Supportive, Critical, Balanced, DevilsAdvocate, ConsensusBuilder (default: Balanced)" },
                        ["expertiseAreas"] = new ClaudeToolProperty { Type = "string", Description = "Comma-separated expertise keywords (e.g., 'finance,budget,revenue,costs')" },
                        ["seniorityLevel"] = new ClaudeToolProperty { Type = "integer", Description = "Seniority 1-5 where 5 is most senior (default: 3)" },
                        ["asksQuestions"] = new ClaudeToolProperty { Type = "boolean", Description = "Whether agent asks clarifying questions (default: false)" },
                        ["givesBriefAcknowledgments"] = new ClaudeToolProperty { Type = "boolean", Description = "Whether agent gives brief acknowledgments (default: true)" },
                        ["isActive"] = new ClaudeToolProperty { Type = "boolean", Description = "Whether agent is active (default: true)" }
                    },
                    Required = new[] { "name", "roleTitle", "systemPrompt" }
                }
            },
            new ClaudeTool
            {
                Name = "update_ai_agent",
                Description = "Update an existing AI agent's configuration",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["agentId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the AI agent to update" },
                        ["name"] = new ClaudeToolProperty { Type = "string", Description = "The agent's display name" },
                        ["roleTitle"] = new ClaudeToolProperty { Type = "string", Description = "The agent's role/title" },
                        ["systemPrompt"] = new ClaudeToolProperty { Type = "string", Description = "The system prompt" },
                        ["provider"] = new ClaudeToolProperty { Type = "string", Description = "AI provider: Anthropic, OpenAI, or Google" },
                        ["modelId"] = new ClaudeToolProperty { Type = "string", Description = "Model ID" },
                        ["modelDisplayName"] = new ClaudeToolProperty { Type = "string", Description = "Display name for the model" },
                        ["avatarColor"] = new ClaudeToolProperty { Type = "string", Description = "Hex color for avatar" },
                        ["maxTokensPerResponse"] = new ClaudeToolProperty { Type = "integer", Description = "Max tokens per response" },
                        ["temperature"] = new ClaudeToolProperty { Type = "number", Description = "Temperature setting 0.0-2.0" },
                        ["assertiveness"] = new ClaudeToolProperty { Type = "integer", Description = "How likely to speak up 0-100" },
                        ["communicationStyle"] = new ClaudeToolProperty { Type = "string", Description = "Style: Formal, Casual, Direct, Diplomatic, Analytical" },
                        ["reactionTendency"] = new ClaudeToolProperty { Type = "string", Description = "Reaction: Supportive, Critical, Balanced, DevilsAdvocate, ConsensusBuilder" },
                        ["expertiseAreas"] = new ClaudeToolProperty { Type = "string", Description = "Comma-separated expertise keywords" },
                        ["seniorityLevel"] = new ClaudeToolProperty { Type = "integer", Description = "Seniority 1-5" },
                        ["asksQuestions"] = new ClaudeToolProperty { Type = "boolean", Description = "Whether agent asks clarifying questions" },
                        ["givesBriefAcknowledgments"] = new ClaudeToolProperty { Type = "boolean", Description = "Whether agent gives brief acknowledgments" },
                        ["isActive"] = new ClaudeToolProperty { Type = "boolean", Description = "Whether agent is active" }
                    },
                    Required = new[] { "agentId" }
                }
            },
            new ClaudeTool
            {
                Name = "delete_ai_agent",
                Description = "Delete (soft-delete) an AI agent",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["agentId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the AI agent to delete" }
                    },
                    Required = new[] { "agentId" }
                }
            },
            new ClaudeTool
            {
                Name = "list_ai_agents",
                Description = "List all AI agents in the organization",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["activeOnly"] = new ClaudeToolProperty { Type = "boolean", Description = "Only return active agents (default: false)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "bulk_create_ai_agents",
                Description = "Create multiple AI agents at once - useful for setting up a complete team",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["agents"] = new ClaudeToolProperty { Type = "array", Description = "Array of agent objects with name, roleTitle, systemPrompt, and optional personality settings" }
                    },
                    Required = new[] { "agents" }
                }
            },
            // ===== Conversation/Group Chat Management Tools =====
            new ClaudeTool
            {
                Name = "create_conversation",
                Description = "Create a new group chat/conversation with specified AI agents",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["title"] = new ClaudeToolProperty { Type = "string", Description = "Title of the conversation (e.g., 'Q4 Strategy Review')" },
                        ["mode"] = new ClaudeToolProperty { Type = "string", Description = "Conversation mode: OnDemand (agents respond when @mentioned), Moderated, RoundRobin, Free, Emergent (default: OnDemand)" },
                        ["agentIds"] = new ClaudeToolProperty { Type = "array", Description = "Array of AI agent IDs to add as participants" },
                        ["maxTurns"] = new ClaudeToolProperty { Type = "integer", Description = "Optional limit on conversation turns" },
                        ["maxTokens"] = new ClaudeToolProperty { Type = "integer", Description = "Optional limit on total tokens" }
                    },
                    Required = new[] { "title" }
                }
            },
            new ClaudeTool
            {
                Name = "update_conversation",
                Description = "Update a conversation's settings",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["conversationId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the conversation to update" },
                        ["title"] = new ClaudeToolProperty { Type = "string", Description = "New title" },
                        ["mode"] = new ClaudeToolProperty { Type = "string", Description = "New mode: OnDemand, Moderated, RoundRobin, Free, Emergent" },
                        ["status"] = new ClaudeToolProperty { Type = "string", Description = "Status: Active, Paused, Archived" },
                        ["maxTurns"] = new ClaudeToolProperty { Type = "integer", Description = "Max turns limit" },
                        ["maxTokens"] = new ClaudeToolProperty { Type = "integer", Description = "Max tokens limit" }
                    },
                    Required = new[] { "conversationId" }
                }
            },
            new ClaudeTool
            {
                Name = "add_agents_to_conversation",
                Description = "Add AI agents as participants to an existing conversation",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["conversationId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the conversation" },
                        ["agentIds"] = new ClaudeToolProperty { Type = "array", Description = "Array of AI agent IDs to add" }
                    },
                    Required = new[] { "conversationId", "agentIds" }
                }
            },
            new ClaudeTool
            {
                Name = "remove_agents_from_conversation",
                Description = "Remove AI agents from a conversation",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["conversationId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the conversation" },
                        ["agentIds"] = new ClaudeToolProperty { Type = "array", Description = "Array of AI agent IDs to remove" }
                    },
                    Required = new[] { "conversationId", "agentIds" }
                }
            },
            new ClaudeTool
            {
                Name = "list_conversations",
                Description = "List all conversations in the organization",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["status"] = new ClaudeToolProperty { Type = "string", Description = "Filter by status: Active, Paused, Archived (default: all)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "delete_conversation",
                Description = "Delete (soft-delete) a conversation",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["conversationId"] = new ClaudeToolProperty { Type = "string", Description = "The ID of the conversation to delete" }
                    },
                    Required = new[] { "conversationId" }
                }
            },
            // Knowledge Base Tools
            new ClaudeTool
            {
                Name = "lookup_knowledge_base",
                Description = "Look up best practices and guidelines from the knowledge base. Use this when the user asks about methodologies, frameworks, or how to do something correctly (e.g., 'What are IE symbols?', 'How do I calculate OEE?', 'Best practices for role design').",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["article_id"] = new ClaudeToolProperty { Type = "string", Description = "The article ID to retrieve (e.g., 'process-mapping/ie-symbols', 'roles/raci-matrix')" },
                        ["search_query"] = new ClaudeToolProperty { Type = "string", Description = "Search for articles by keyword if you don't know the exact ID" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            // ===== Data Query Tools =====
            // These tools allow the AI to fetch organization data on-demand instead of having it pre-loaded
            new ClaudeTool
            {
                Name = "get_people",
                Description = "Get list of people in the organization with their roles and capabilities. Use this when you need to see who works in the organization, their roles, or their skills.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["search"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter people by name (case-insensitive partial match)" },
                        ["roleId"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter people by role ID" },
                        ["limit"] = new ClaudeToolProperty { Type = "integer", Description = "Optional: Maximum number of results (default: 50)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "get_roles",
                Description = "Get list of roles in the organization with their departments and assignment counts. Use this when you need to see what roles exist or find a specific role.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["search"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter roles by name (case-insensitive partial match)" },
                        ["department"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter roles by department" },
                        ["limit"] = new ClaudeToolProperty { Type = "integer", Description = "Optional: Maximum number of results (default: 50)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "get_functions",
                Description = "Get list of business functions (skills/capabilities) in the organization. Use this when you need to see what functions exist or find functions in a category.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["search"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter functions by name (case-insensitive partial match)" },
                        ["category"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter functions by category" },
                        ["limit"] = new ClaudeToolProperty { Type = "integer", Description = "Optional: Maximum number of results (default: 50)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "get_processes",
                Description = "Get list of business processes in the organization. Use this when you need to see what processes exist.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["search"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter processes by name (case-insensitive partial match)" },
                        ["status"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by status (Active, Draft, Deprecated)" },
                        ["limit"] = new ClaudeToolProperty { Type = "integer", Description = "Optional: Maximum number of results (default: 50)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "get_goals",
                Description = "Get list of goals, objectives, and key results in the organization. Use this when you need to see what goals exist.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["search"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter goals by name (case-insensitive partial match)" },
                        ["goalType"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by type (Objective, KeyResult, Initiative)" },
                        ["status"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by status" },
                        ["limit"] = new ClaudeToolProperty { Type = "integer", Description = "Optional: Maximum number of results (default: 50)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "get_partners",
                Description = "Get list of partners (suppliers, distributors, strategic partners) in the organization.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["search"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter partners by name (case-insensitive partial match)" },
                        ["type"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by type (Supplier, Distributor, Strategic, etc.)" },
                        ["limit"] = new ClaudeToolProperty { Type = "integer", Description = "Optional: Maximum number of results (default: 50)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "get_channels",
                Description = "Get list of channels (sales, marketing, distribution) in the organization.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["search"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter channels by name (case-insensitive partial match)" },
                        ["type"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by type (Direct, Indirect, Digital, Physical, Hybrid)" },
                        ["category"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by category (Sales, Marketing, Distribution, Support, Communication)" },
                        ["limit"] = new ClaudeToolProperty { Type = "integer", Description = "Optional: Maximum number of results (default: 50)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "get_value_propositions",
                Description = "Get list of value propositions in the organization.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["search"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by name (case-insensitive partial match)" },
                        ["limit"] = new ClaudeToolProperty { Type = "integer", Description = "Optional: Maximum number of results (default: 50)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "get_customer_relationships",
                Description = "Get list of customer relationship types in the organization.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["search"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by name (case-insensitive partial match)" },
                        ["type"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by type" },
                        ["limit"] = new ClaudeToolProperty { Type = "integer", Description = "Optional: Maximum number of results (default: 50)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "get_revenue_streams",
                Description = "Get list of revenue streams in the organization.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["search"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by name (case-insensitive partial match)" },
                        ["type"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by type" },
                        ["limit"] = new ClaudeToolProperty { Type = "integer", Description = "Optional: Maximum number of results (default: 50)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "get_canvases",
                Description = "Get list of business model canvases in the organization.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["search"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by name (case-insensitive partial match)" },
                        ["canvasType"] = new ClaudeToolProperty { Type = "string", Description = "Optional: Filter by canvas type" },
                        ["limit"] = new ClaudeToolProperty { Type = "integer", Description = "Optional: Maximum number of results (default: 50)" }
                    },
                    Required = Array.Empty<string>()
                }
            },
            new ClaudeTool
            {
                Name = "get_full_context",
                Description = "Get ALL organization data at once. ONLY use this when the user explicitly asks for a comprehensive overview or full analysis. This is expensive - prefer specific query tools for targeted questions.",
                InputSchema = new ClaudeToolSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, ClaudeToolProperty>
                    {
                        ["confirm"] = new ClaudeToolProperty { Type = "boolean", Description = "Set to true to confirm you want to fetch all data" }
                    },
                    Required = new[] { "confirm" }
                }
            }
        };
    }

    private async Task<AiChatResponse> ProcessResponseAsync(
        Guid organizationId,
        ClaudeResponse response,
        List<ClaudeMessage> messages,
        List<ClaudeTool> tools,
        string systemPrompt,
        CancellationToken cancellationToken)
    {
        var toolCalls = new List<AiToolCall>();
        var textContent = new StringBuilder();

        var toolUses = response.Content.Where(c => c.Type == "tool_use").ToList();

        if (toolUses.Any())
        {
            var toolResults = new List<object>();
            foreach (var toolUse in toolUses)
            {
                var result = await ExecuteToolAsync(organizationId, toolUse.Name!, toolUse.Input, cancellationToken);
                toolCalls.Add(new AiToolCall
                {
                    Tool = toolUse.Name!,
                    Action = result.Action,
                    Data = result.Data
                });

                toolResults.Add(new
                {
                    type = "tool_result",
                    tool_use_id = toolUse.Id,
                    content = result.Message ?? "Action completed"
                });
            }

            // Add assistant message with tool uses
            messages.Add(new ClaudeMessage
            {
                Role = "assistant",
                Content = response.Content.Select(c => (object)new
                {
                    type = c.Type,
                    id = c.Id,
                    name = c.Name,
                    input = c.Input,
                    text = c.Text
                }).ToList()
            });

            // Add tool results
            messages.Add(new ClaudeMessage
            {
                Role = "user",
                Content = toolResults
            });

            // Get final response
            var followUpResponse = await CallClaudeApiAsync(systemPrompt, messages, tools, cancellationToken);

            foreach (var content in followUpResponse.Content)
            {
                if (content.Type == "text")
                    textContent.Append(content.Text);
            }
        }
        else
        {
            foreach (var content in response.Content)
            {
                if (content.Type == "text")
                    textContent.Append(content.Text);
            }
        }

        return new AiChatResponse
        {
            Message = textContent.ToString().Trim(),
            ToolCalls = toolCalls.Any() ? toolCalls : null
        };
    }

    private async Task<ToolResult> ExecuteToolAsync(
        Guid organizationId,
        string toolName,
        JsonElement? input,
        CancellationToken cancellationToken)
    {
        try
        {
            return toolName switch
            {
                "create_person" => await CreatePersonAsync(organizationId, input, cancellationToken),
                "update_person" => await UpdatePersonAsync(organizationId, input, cancellationToken),
                "assign_role" => await AssignRoleAsync(organizationId, input, cancellationToken),
                "add_capability" => await AddCapabilityAsync(organizationId, input, cancellationToken),
                "analyze_coverage" => await AnalyzeCoverageAsync(organizationId, cancellationToken),
                // Function management tools
                "create_function" => await CreateFunctionAsync(organizationId, input, cancellationToken),
                "update_function" => await UpdateFunctionAsync(organizationId, input, cancellationToken),
                "bulk_create_functions" => await BulkCreateFunctionsAsync(organizationId, input, cancellationToken),
                "suggest_functions" => await SuggestFunctionsAsync(organizationId, input, cancellationToken),
                "suggest_improvements" => await SuggestImprovementsAsync(organizationId, cancellationToken),
                "delete_function" => await DeleteFunctionAsync(organizationId, input, cancellationToken),
                // Process management tools
                "create_process" => await CreateProcessAsync(organizationId, input, cancellationToken),
                "bulk_create_processes" => await BulkCreateProcessesAsync(organizationId, input, cancellationToken),
                "add_activities_to_process" => await AddActivitiesToProcessAsync(organizationId, input, cancellationToken),
                // Goal management tools
                "create_goal" => await CreateGoalAsync(organizationId, input, cancellationToken),
                "bulk_create_goals" => await BulkCreateGoalsAsync(organizationId, input, cancellationToken),
                // Partner management tools
                "create_partner" => await CreatePartnerAsync(organizationId, input, cancellationToken),
                "bulk_create_partners" => await BulkCreatePartnersAsync(organizationId, input, cancellationToken),
                // Channel management tools
                "create_channel" => await CreateChannelAsync(organizationId, input, cancellationToken),
                "bulk_create_channels" => await BulkCreateChannelsAsync(organizationId, input, cancellationToken),
                // Value proposition management tools
                "create_value_proposition" => await CreateValuePropositionAsync(organizationId, input, cancellationToken),
                "bulk_create_value_propositions" => await BulkCreateValuePropositionsAsync(organizationId, input, cancellationToken),
                // Customer relationship management tools
                "create_customer_relationship" => await CreateCustomerRelationshipAsync(organizationId, input, cancellationToken),
                "bulk_create_customer_relationships" => await BulkCreateCustomerRelationshipsAsync(organizationId, input, cancellationToken),
                // Revenue stream management tools
                "create_revenue_stream" => await CreateRevenueStreamAsync(organizationId, input, cancellationToken),
                "bulk_create_revenue_streams" => await BulkCreateRevenueStreamsAsync(organizationId, input, cancellationToken),
                // Role management tools
                "create_role" => await CreateRoleAsync(organizationId, input, cancellationToken),
                "bulk_create_roles" => await BulkCreateRolesAsync(organizationId, input, cancellationToken),
                // AI Agent management tools
                "create_ai_agent" => await CreateAiAgentAsync(organizationId, input, cancellationToken),
                "update_ai_agent" => await UpdateAiAgentAsync(organizationId, input, cancellationToken),
                "delete_ai_agent" => await DeleteAiAgentAsync(organizationId, input, cancellationToken),
                "list_ai_agents" => await ListAiAgentsAsync(organizationId, input, cancellationToken),
                "bulk_create_ai_agents" => await BulkCreateAiAgentsAsync(organizationId, input, cancellationToken),
                // Conversation management tools
                "create_conversation" => await CreateConversationAsync(organizationId, input, cancellationToken),
                "update_conversation" => await UpdateConversationAsync(organizationId, input, cancellationToken),
                "add_agents_to_conversation" => await AddAgentsToConversationAsync(organizationId, input, cancellationToken),
                "remove_agents_from_conversation" => await RemoveAgentsFromConversationAsync(organizationId, input, cancellationToken),
                "list_conversations" => await ListConversationsAsync(organizationId, input, cancellationToken),
                "delete_conversation" => await DeleteConversationAsync(organizationId, input, cancellationToken),
                // Knowledge Base tools
                "lookup_knowledge_base" => LookupKnowledgeBaseAsync(input),
                // Data Query tools
                "get_people" => await GetPeopleAsync(organizationId, input, cancellationToken),
                "get_roles" => await GetRolesAsync(organizationId, input, cancellationToken),
                "get_functions" => await GetFunctionsAsync(organizationId, input, cancellationToken),
                "get_processes" => await GetProcessesAsync(organizationId, input, cancellationToken),
                "get_goals" => await GetGoalsAsync(organizationId, input, cancellationToken),
                "get_partners" => await GetPartnersAsync(organizationId, input, cancellationToken),
                "get_channels" => await GetChannelsAsync(organizationId, input, cancellationToken),
                "get_value_propositions" => await GetValuePropositionsAsync(organizationId, input, cancellationToken),
                "get_customer_relationships" => await GetCustomerRelationshipsAsync(organizationId, input, cancellationToken),
                "get_revenue_streams" => await GetRevenueStreamsAsync(organizationId, input, cancellationToken),
                "get_canvases" => await GetCanvasesAsync(organizationId, input, cancellationToken),
                "get_full_context" => await GetFullContextAsync(organizationId, input, cancellationToken),
                _ => new ToolResult { Action = "unknown", Message = $"Unknown tool: {toolName}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {Tool}", toolName);
            return new ToolResult { Action = "error", Message = $"Error: {ex.Message}" };
        }
    }

    private ToolResult LookupKnowledgeBaseAsync(JsonElement? input)
    {
        // Check if article_id is provided for direct lookup
        if (input?.TryGetProperty("article_id", out var articleIdProp) == true)
        {
            var articleId = articleIdProp.GetString();
            if (!string.IsNullOrEmpty(articleId))
            {
                var article = _knowledgeBaseService.GetArticle(articleId);
                if (article != null)
                {
                    return new ToolResult
                    {
                        Action = "found",
                        Message = $"# {article.Title}\n\n{article.Content}",
                        Data = JsonSerializer.SerializeToElement(new
                        {
                            id = article.Id,
                            title = article.Title,
                            category = article.Category,
                            relatedArticles = article.RelatedArticles
                        })
                    };
                }
                return new ToolResult
                {
                    Action = "not_found",
                    Message = $"Article '{articleId}' not found in the knowledge base."
                };
            }
        }

        // Check if search_query is provided for search
        if (input?.TryGetProperty("search_query", out var searchProp) == true)
        {
            var query = searchProp.GetString();
            if (!string.IsNullOrEmpty(query))
            {
                var results = _knowledgeBaseService.SearchArticles(query, 5);
                if (results.Count > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Found {results.Count} articles matching '{query}':\n");

                    foreach (var article in results)
                    {
                        sb.AppendLine($"## {article.Title}");
                        sb.AppendLine($"**ID:** `{article.Id}`");
                        sb.AppendLine($"**Summary:** {article.Summary}");
                        sb.AppendLine();
                    }

                    sb.AppendLine("\nUse `lookup_knowledge_base` with the `article_id` to get the full content of any article.");

                    return new ToolResult
                    {
                        Action = "search_results",
                        Message = sb.ToString(),
                        Data = JsonSerializer.SerializeToElement(results.Select(a => new
                        {
                            id = a.Id,
                            title = a.Title,
                            summary = a.Summary
                        }))
                    };
                }
                return new ToolResult
                {
                    Action = "no_results",
                    Message = $"No articles found matching '{query}'."
                };
            }
        }

        // No valid input - return available topics
        var index = _knowledgeBaseService.GetIndex();
        var topicsSb = new StringBuilder();
        topicsSb.AppendLine("Please provide either an `article_id` or `search_query`. Available topics:\n");

        foreach (var category in index.Categories)
        {
            topicsSb.AppendLine($"**{category.Name}**:");
            foreach (var article in category.Articles)
            {
                topicsSb.AppendLine($"  - `{article.Id}`: {article.Title}");
            }
            topicsSb.AppendLine();
        }

        return new ToolResult
        {
            Action = "help",
            Message = topicsSb.ToString()
        };
    }

    #region Data Query Tools

    private async Task<ToolResult> GetPeopleAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var search = input?.TryGetProperty("search", out var searchProp) == true ? searchProp.GetString() : null;
        var roleId = input?.TryGetProperty("roleId", out var roleIdProp) == true ? roleIdProp.GetString() : null;
        var limit = input?.TryGetProperty("limit", out var limitProp) == true ? limitProp.GetInt32() : 50;

        var query = _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .Include(r => r.RoleAssignments)
                .ThenInclude(ra => ra.Role)
            .Include(r => r.FunctionCapabilities)
                .ThenInclude(fc => fc.Function)
            .Where(r => r.OrganizationId == organizationId && r.ResourceSubtype.ResourceType == ResourceType.Person);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(r => r.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrEmpty(roleId) && Guid.TryParse(roleId, out var roleGuid))
            query = query.Where(r => r.RoleAssignments.Any(ra => ra.RoleId == roleGuid));

        var people = await query.Take(limit).ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## People ({people.Count} results)\n");

        foreach (var person in people)
        {
            sb.AppendLine($"### {person.Name}");
            sb.AppendLine($"- **ID:** `{person.Id}`");
            sb.AppendLine($"- **Status:** {person.Status}");
            if (!string.IsNullOrEmpty(person.Description))
                sb.AppendLine($"- **Description:** {person.Description}");
            if (person.RoleAssignments.Any())
                sb.AppendLine($"- **Roles:** {string.Join(", ", person.RoleAssignments.Select(ra => $"{ra.Role.Name}{(ra.IsPrimary ? " (Primary)" : "")}"))}");
            if (person.FunctionCapabilities.Any())
                sb.AppendLine($"- **Capabilities:** {string.Join(", ", person.FunctionCapabilities.Select(fc => $"{fc.Function.Name} ({fc.Level})"))}");
            sb.AppendLine();
        }

        return new ToolResult
        {
            Action = "query_results",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(people.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                status = p.Status.ToString(),
                description = p.Description,
                roles = p.RoleAssignments.Select(ra => new { id = ra.RoleId, name = ra.Role.Name, isPrimary = ra.IsPrimary }),
                capabilities = p.FunctionCapabilities.Select(fc => new { functionId = fc.FunctionId, name = fc.Function.Name, level = fc.Level.ToString() })
            }))
        };
    }

    private async Task<ToolResult> GetRolesAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var search = input?.TryGetProperty("search", out var searchProp) == true ? searchProp.GetString() : null;
        var department = input?.TryGetProperty("department", out var deptProp) == true ? deptProp.GetString() : null;
        var limit = input?.TryGetProperty("limit", out var limitProp) == true ? limitProp.GetInt32() : 50;

        var query = _dbContext.Roles.Where(r => r.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(r => r.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrEmpty(department))
            query = query.Where(r => r.Department != null && r.Department.ToLower().Contains(department.ToLower()));

        var roles = await query
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.Department,
                AssignmentCount = _dbContext.RoleAssignments.Count(ra => ra.RoleId == r.Id)
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Roles ({roles.Count} results)\n");

        foreach (var role in roles)
        {
            sb.AppendLine($"- **{role.Name}** (ID: `{role.Id}`)");
            if (!string.IsNullOrEmpty(role.Department))
                sb.AppendLine($"  - Department: {role.Department}");
            sb.AppendLine($"  - Assignments: {role.AssignmentCount} people");
            if (!string.IsNullOrEmpty(role.Description))
                sb.AppendLine($"  - Description: {role.Description}");
        }

        return new ToolResult
        {
            Action = "query_results",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(roles)
        };
    }

    private async Task<ToolResult> GetFunctionsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var search = input?.TryGetProperty("search", out var searchProp) == true ? searchProp.GetString() : null;
        var category = input?.TryGetProperty("category", out var catProp) == true ? catProp.GetString() : null;
        var limit = input?.TryGetProperty("limit", out var limitProp) == true ? limitProp.GetInt32() : 50;

        var query = _dbContext.Functions.Where(f => f.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(f => f.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrEmpty(category))
            query = query.Where(f => f.Category != null && f.Category.ToLower().Contains(category.ToLower()));

        var functions = await query
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Description,
                f.Category,
                f.Purpose,
                CapabilityCount = f.FunctionCapabilities.Count
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Functions ({functions.Count} results)\n");

        var grouped = functions.GroupBy(f => f.Category ?? "Uncategorized");
        foreach (var group in grouped)
        {
            sb.AppendLine($"### {group.Key}");
            foreach (var func in group)
            {
                sb.AppendLine($"- **{func.Name}** (ID: `{func.Id}`, {func.CapabilityCount} capable)");
                if (!string.IsNullOrEmpty(func.Description))
                    sb.AppendLine($"  - {func.Description}");
            }
            sb.AppendLine();
        }

        return new ToolResult
        {
            Action = "query_results",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(functions)
        };
    }

    private async Task<ToolResult> GetProcessesAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var search = input?.TryGetProperty("search", out var searchProp) == true ? searchProp.GetString() : null;
        var statusStr = input?.TryGetProperty("status", out var statusProp) == true ? statusProp.GetString() : null;
        var limit = input?.TryGetProperty("limit", out var limitProp) == true ? limitProp.GetInt32() : 50;

        var query = _dbContext.Processes
            .Include(p => p.Owner)
            .Where(p => p.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrEmpty(statusStr) && Enum.TryParse<ProcessStatus>(statusStr, true, out var status))
            query = query.Where(p => p.Status == status);

        var processes = await query
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Purpose,
                p.Trigger,
                p.Output,
                Status = p.Status.ToString(),
                OwnerName = p.Owner != null ? p.Owner.Name : null,
                ActivityCount = p.Activities.Count
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Processes ({processes.Count} results)\n");

        foreach (var process in processes)
        {
            sb.AppendLine($"### {process.Name}");
            sb.AppendLine($"- **ID:** `{process.Id}`");
            sb.AppendLine($"- **Status:** {process.Status}");
            sb.AppendLine($"- **Activities:** {process.ActivityCount}");
            if (!string.IsNullOrEmpty(process.OwnerName))
                sb.AppendLine($"- **Owner:** {process.OwnerName}");
            if (!string.IsNullOrEmpty(process.Purpose))
                sb.AppendLine($"- **Purpose:** {process.Purpose}");
            if (!string.IsNullOrEmpty(process.Trigger))
                sb.AppendLine($"- **Trigger:** {process.Trigger}");
            if (!string.IsNullOrEmpty(process.Output))
                sb.AppendLine($"- **Output:** {process.Output}");
            sb.AppendLine();
        }

        return new ToolResult
        {
            Action = "query_results",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(processes)
        };
    }

    private async Task<ToolResult> GetGoalsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var search = input?.TryGetProperty("search", out var searchProp) == true ? searchProp.GetString() : null;
        var goalTypeStr = input?.TryGetProperty("goalType", out var typeProp) == true ? typeProp.GetString() : null;
        var statusStr = input?.TryGetProperty("status", out var statusProp) == true ? statusProp.GetString() : null;
        var limit = input?.TryGetProperty("limit", out var limitProp) == true ? limitProp.GetInt32() : 50;

        var query = _dbContext.Goals
            .Include(g => g.Owner)
            .Where(g => g.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(g => g.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrEmpty(goalTypeStr) && Enum.TryParse<GoalType>(goalTypeStr, true, out var goalType))
            query = query.Where(g => g.GoalType == goalType);

        if (!string.IsNullOrEmpty(statusStr) && Enum.TryParse<GoalStatus>(statusStr, true, out var status))
            query = query.Where(g => g.Status == status);

        var goals = await query
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                GoalType = g.GoalType.ToString(),
                Status = g.Status.ToString(),
                OwnerName = g.Owner != null ? g.Owner.Name : null,
                g.TargetValue,
                g.CurrentValue,
                g.Unit
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Goals ({goals.Count} results)\n");

        foreach (var goal in goals)
        {
            sb.AppendLine($"### {goal.Name}");
            sb.AppendLine($"- **ID:** `{goal.Id}`");
            sb.AppendLine($"- **Type:** {goal.GoalType}");
            sb.AppendLine($"- **Status:** {goal.Status}");
            if (!string.IsNullOrEmpty(goal.OwnerName))
                sb.AppendLine($"- **Owner:** {goal.OwnerName}");
            if (goal.TargetValue.HasValue)
                sb.AppendLine($"- **Target:** {goal.TargetValue} {goal.Unit}");
            if (goal.CurrentValue.HasValue)
                sb.AppendLine($"- **Current:** {goal.CurrentValue} {goal.Unit}");
            if (!string.IsNullOrEmpty(goal.Description))
                sb.AppendLine($"- **Description:** {goal.Description}");
            sb.AppendLine();
        }

        return new ToolResult
        {
            Action = "query_results",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(goals)
        };
    }

    private async Task<ToolResult> GetPartnersAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var search = input?.TryGetProperty("search", out var searchProp) == true ? searchProp.GetString() : null;
        var typeStr = input?.TryGetProperty("type", out var typeProp) == true ? typeProp.GetString() : null;
        var limit = input?.TryGetProperty("limit", out var limitProp) == true ? limitProp.GetInt32() : 50;

        var query = _dbContext.Partners.Where(p => p.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrEmpty(typeStr) && Enum.TryParse<PartnerType>(typeStr, true, out var partnerType))
            query = query.Where(p => p.Type == partnerType);

        var partners = await query
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                Type = p.Type.ToString(),
                Status = p.Status.ToString(),
                StrategicValue = p.StrategicValue.ToString()
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Partners ({partners.Count} results)\n");

        foreach (var partner in partners)
        {
            sb.AppendLine($"- **{partner.Name}** (ID: `{partner.Id}`)");
            sb.AppendLine($"  - Type: {partner.Type}, Status: {partner.Status}");
            sb.AppendLine($"  - Strategic Value: {partner.StrategicValue}");
            if (!string.IsNullOrEmpty(partner.Description))
                sb.AppendLine($"  - {partner.Description}");
        }

        return new ToolResult
        {
            Action = "query_results",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(partners)
        };
    }

    private async Task<ToolResult> GetChannelsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var search = input?.TryGetProperty("search", out var searchProp) == true ? searchProp.GetString() : null;
        var typeStr = input?.TryGetProperty("type", out var typeProp) == true ? typeProp.GetString() : null;
        var categoryStr = input?.TryGetProperty("category", out var catProp) == true ? catProp.GetString() : null;
        var limit = input?.TryGetProperty("limit", out var limitProp) == true ? limitProp.GetInt32() : 50;

        var query = _dbContext.Channels
            .Include(c => c.Partner)
            .Where(c => c.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrEmpty(typeStr) && Enum.TryParse<ChannelType>(typeStr, true, out var channelType))
            query = query.Where(c => c.Type == channelType);

        if (!string.IsNullOrEmpty(categoryStr) && Enum.TryParse<ChannelCategory>(categoryStr, true, out var category))
            query = query.Where(c => c.Category == category);

        var channels = await query
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                Type = c.Type.ToString(),
                Category = c.Category.ToString(),
                Status = c.Status.ToString(),
                Ownership = c.Ownership.ToString(),
                PartnerName = c.Partner != null ? c.Partner.Name : null
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Channels ({channels.Count} results)\n");

        foreach (var channel in channels)
        {
            sb.AppendLine($"- **{channel.Name}** (ID: `{channel.Id}`)");
            sb.AppendLine($"  - Type: {channel.Type}, Category: {channel.Category}");
            sb.AppendLine($"  - Status: {channel.Status}, Ownership: {channel.Ownership}");
            if (!string.IsNullOrEmpty(channel.PartnerName))
                sb.AppendLine($"  - Partner: {channel.PartnerName}");
            if (!string.IsNullOrEmpty(channel.Description))
                sb.AppendLine($"  - {channel.Description}");
        }

        return new ToolResult
        {
            Action = "query_results",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(channels)
        };
    }

    private async Task<ToolResult> GetValuePropositionsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var search = input?.TryGetProperty("search", out var searchProp) == true ? searchProp.GetString() : null;
        var limit = input?.TryGetProperty("limit", out var limitProp) == true ? limitProp.GetInt32() : 50;

        var query = _dbContext.ValuePropositions.Where(v => v.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(v => v.Name.ToLower().Contains(search.ToLower()));

        var valueProps = await query
            .Select(v => new
            {
                v.Id,
                v.Name,
                v.Headline,
                v.Description,
                Status = v.Status.ToString()
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Value Propositions ({valueProps.Count} results)\n");

        foreach (var vp in valueProps)
        {
            sb.AppendLine($"### {vp.Name}");
            sb.AppendLine($"- **ID:** `{vp.Id}`");
            sb.AppendLine($"- **Headline:** {vp.Headline}");
            sb.AppendLine($"- **Status:** {vp.Status}");
            if (!string.IsNullOrEmpty(vp.Description))
                sb.AppendLine($"- {vp.Description}");
            sb.AppendLine();
        }

        return new ToolResult
        {
            Action = "query_results",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(valueProps)
        };
    }

    private async Task<ToolResult> GetCustomerRelationshipsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var search = input?.TryGetProperty("search", out var searchProp) == true ? searchProp.GetString() : null;
        var typeStr = input?.TryGetProperty("type", out var typeProp) == true ? typeProp.GetString() : null;
        var limit = input?.TryGetProperty("limit", out var limitProp) == true ? limitProp.GetInt32() : 50;

        var query = _dbContext.CustomerRelationships.Where(cr => cr.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(cr => cr.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrEmpty(typeStr) && Enum.TryParse<CustomerRelationshipType>(typeStr, true, out var crType))
            query = query.Where(cr => cr.Type == crType);

        var customerRels = await query
            .Select(cr => new
            {
                cr.Id,
                cr.Name,
                cr.Description,
                Type = cr.Type.ToString(),
                Status = cr.Status.ToString()
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Customer Relationships ({customerRels.Count} results)\n");

        foreach (var cr in customerRels)
        {
            sb.AppendLine($"- **{cr.Name}** (ID: `{cr.Id}`)");
            sb.AppendLine($"  - Type: {cr.Type}, Status: {cr.Status}");
            if (!string.IsNullOrEmpty(cr.Description))
                sb.AppendLine($"  - {cr.Description}");
        }

        return new ToolResult
        {
            Action = "query_results",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(customerRels)
        };
    }

    private async Task<ToolResult> GetRevenueStreamsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var search = input?.TryGetProperty("search", out var searchProp) == true ? searchProp.GetString() : null;
        var typeStr = input?.TryGetProperty("type", out var typeProp) == true ? typeProp.GetString() : null;
        var limit = input?.TryGetProperty("limit", out var limitProp) == true ? limitProp.GetInt32() : 50;

        var query = _dbContext.RevenueStreams.Where(rs => rs.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(rs => rs.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrEmpty(typeStr) && Enum.TryParse<RevenueStreamType>(typeStr, true, out var rsType))
            query = query.Where(rs => rs.Type == rsType);

        var revenueStreams = await query
            .Select(rs => new
            {
                rs.Id,
                rs.Name,
                rs.Description,
                Type = rs.Type.ToString(),
                Status = rs.Status.ToString(),
                PricingMechanism = rs.PricingMechanism.ToString()
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Revenue Streams ({revenueStreams.Count} results)\n");

        foreach (var rs in revenueStreams)
        {
            sb.AppendLine($"- **{rs.Name}** (ID: `{rs.Id}`)");
            sb.AppendLine($"  - Type: {rs.Type}, Status: {rs.Status}");
            sb.AppendLine($"  - Pricing: {rs.PricingMechanism}");
            if (!string.IsNullOrEmpty(rs.Description))
                sb.AppendLine($"  - {rs.Description}");
        }

        return new ToolResult
        {
            Action = "query_results",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(revenueStreams)
        };
    }

    private async Task<ToolResult> GetCanvasesAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var search = input?.TryGetProperty("search", out var searchProp) == true ? searchProp.GetString() : null;
        var canvasTypeStr = input?.TryGetProperty("canvasType", out var typeProp) == true ? typeProp.GetString() : null;
        var limit = input?.TryGetProperty("limit", out var limitProp) == true ? limitProp.GetInt32() : 50;

        var query = _dbContext.Canvases.Where(c => c.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrEmpty(canvasTypeStr) && Enum.TryParse<CanvasType>(canvasTypeStr, true, out var canvasType))
            query = query.Where(c => c.CanvasType == canvasType);

        var canvases = await query
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                CanvasType = c.CanvasType.ToString(),
                Status = c.Status.ToString(),
                BlockCount = c.Blocks.Count
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Canvases ({canvases.Count} results)\n");

        foreach (var canvas in canvases)
        {
            sb.AppendLine($"- **{canvas.Name}** (ID: `{canvas.Id}`)");
            sb.AppendLine($"  - Type: {canvas.CanvasType}, Status: {canvas.Status}");
            sb.AppendLine($"  - Blocks: {canvas.BlockCount}");
            if (!string.IsNullOrEmpty(canvas.Description))
                sb.AppendLine($"  - {canvas.Description}");
        }

        return new ToolResult
        {
            Action = "query_results",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(canvases)
        };
    }

    private async Task<ToolResult> GetFullContextAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var confirm = input?.TryGetProperty("confirm", out var confirmProp) == true && confirmProp.GetBoolean();

        if (!confirm)
        {
            return new ToolResult
            {
                Action = "confirmation_required",
                Message = "Getting full context will fetch ALL organization data. Set confirm=true to proceed. Consider using specific query tools (get_people, get_roles, etc.) if you only need partial data."
            };
        }

        // Fetch ALL data - use the original queries from OrganizationContextService
        var sb = new StringBuilder();
        sb.AppendLine("# Full Organization Context\n");

        // People
        var people = await _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .Include(r => r.RoleAssignments)
                .ThenInclude(ra => ra.Role)
            .Include(r => r.FunctionCapabilities)
                .ThenInclude(fc => fc.Function)
            .Where(r => r.OrganizationId == organizationId && r.ResourceSubtype.ResourceType == ResourceType.Person)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"## People ({people.Count})\n");
        foreach (var person in people)
        {
            sb.AppendLine($"- **{person.Name}** (ID: `{person.Id}`, Status: {person.Status})");
            if (person.RoleAssignments.Any())
                sb.AppendLine($"  - Roles: {string.Join(", ", person.RoleAssignments.Select(ra => ra.Role.Name))}");
            if (person.FunctionCapabilities.Any())
                sb.AppendLine($"  - Capabilities: {string.Join(", ", person.FunctionCapabilities.Select(fc => fc.Function.Name))}");
        }
        sb.AppendLine();

        // Roles
        var roles = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"## Roles ({roles.Count})\n");
        foreach (var role in roles)
            sb.AppendLine($"- **{role.Name}** (ID: `{role.Id}`){(role.Department != null ? $" - {role.Department}" : "")}");
        sb.AppendLine();

        // Functions
        var functions = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"## Functions ({functions.Count})\n");
        var funcGrouped = functions.GroupBy(f => f.Category ?? "Uncategorized");
        foreach (var group in funcGrouped)
        {
            sb.AppendLine($"**{group.Key}:** {string.Join(", ", group.Select(f => f.Name))}");
        }
        sb.AppendLine();

        // Processes
        var processes = await _dbContext.Processes
            .Include(p => p.Owner)
            .Where(p => p.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"## Processes ({processes.Count})\n");
        foreach (var process in processes)
            sb.AppendLine($"- **{process.Name}** (ID: `{process.Id}`, Owner: {process.Owner?.Name ?? "Unassigned"})");
        sb.AppendLine();

        // Goals
        var goals = await _dbContext.Goals
            .Where(g => g.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"## Goals ({goals.Count})\n");
        foreach (var goal in goals)
            sb.AppendLine($"- **{goal.Name}** ({goal.GoalType}, Status: {goal.Status})");
        sb.AppendLine();

        // Partners
        var partners = await _dbContext.Partners
            .Where(p => p.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"## Partners ({partners.Count})\n");
        foreach (var partner in partners)
            sb.AppendLine($"- **{partner.Name}** ({partner.Type})");
        sb.AppendLine();

        // Channels
        var channels = await _dbContext.Channels
            .Where(c => c.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"## Channels ({channels.Count})\n");
        foreach (var channel in channels)
            sb.AppendLine($"- **{channel.Name}** ({channel.Type}, {channel.Category})");
        sb.AppendLine();

        // Value Propositions
        var valueProps = await _dbContext.ValuePropositions
            .Where(v => v.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"## Value Propositions ({valueProps.Count})\n");
        foreach (var vp in valueProps)
            sb.AppendLine($"- **{vp.Name}**: {vp.Headline}");
        sb.AppendLine();

        // Customer Relationships
        var customerRels = await _dbContext.CustomerRelationships
            .Where(cr => cr.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"## Customer Relationships ({customerRels.Count})\n");
        foreach (var cr in customerRels)
            sb.AppendLine($"- **{cr.Name}** ({cr.Type})");
        sb.AppendLine();

        // Revenue Streams
        var revenueStreams = await _dbContext.RevenueStreams
            .Where(rs => rs.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"## Revenue Streams ({revenueStreams.Count})\n");
        foreach (var rs in revenueStreams)
            sb.AppendLine($"- **{rs.Name}** ({rs.Type}, {rs.PricingMechanism})");

        return new ToolResult
        {
            Action = "full_context",
            Message = sb.ToString()
        };
    }

    #endregion

    private async Task<ToolResult> CreatePersonAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var name = input.Value.GetProperty("name").GetString()!;
        var description = input.Value.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
        var subtypeId = Guid.Parse(input.Value.GetProperty("resourceSubtypeId").GetString()!);

        var subtype = await _dbContext.ResourceSubtypes
            .FirstOrDefaultAsync(s => s.Id == subtypeId && s.OrganizationId == organizationId, cancellationToken);

        if (subtype == null)
            return new ToolResult { Action = "error", Message = "Invalid person type specified." };

        var resource = new Resource
        {
            Name = name,
            Description = description,
            Status = ResourceStatus.Active,
            OrganizationId = organizationId,
            ResourceSubtypeId = subtypeId
        };

        _dbContext.Resources.Add(resource);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "created",
            Message = $"Successfully created **{name}** as a {subtype.Name}.",
            Data = JsonSerializer.SerializeToElement(new { id = resource.Id, name = resource.Name })
        };
    }

    private async Task<ToolResult> UpdatePersonAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var personId = Guid.Parse(input.Value.GetProperty("personId").GetString()!);
        var person = await _dbContext.Resources
            .FirstOrDefaultAsync(r => r.Id == personId && r.OrganizationId == organizationId, cancellationToken);

        if (person == null)
            return new ToolResult { Action = "error", Message = "Person not found." };

        if (input.Value.TryGetProperty("name", out var nameProp))
            person.Name = nameProp.GetString()!;

        if (input.Value.TryGetProperty("description", out var descProp))
            person.Description = descProp.GetString();

        if (input.Value.TryGetProperty("status", out var statusProp))
        {
            if (Enum.TryParse<ResourceStatus>(statusProp.GetString(), true, out var status))
                person.Status = status;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "updated",
            Message = $"Successfully updated **{person.Name}**.",
            Data = JsonSerializer.SerializeToElement(new { id = person.Id, name = person.Name })
        };
    }

    private async Task<ToolResult> AssignRoleAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var personId = Guid.Parse(input.Value.GetProperty("personId").GetString()!);
        var roleId = Guid.Parse(input.Value.GetProperty("roleId").GetString()!);
        var allocation = input.Value.TryGetProperty("allocationPercentage", out var allocProp) ? allocProp.GetDecimal() : 100m;
        var isPrimary = input.Value.TryGetProperty("isPrimary", out var primaryProp) && primaryProp.GetBoolean();

        var person = await _dbContext.Resources
            .FirstOrDefaultAsync(r => r.Id == personId && r.OrganizationId == organizationId, cancellationToken);
        if (person == null)
            return new ToolResult { Action = "error", Message = "Person not found." };

        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.OrganizationId == organizationId, cancellationToken);
        if (role == null)
            return new ToolResult { Action = "error", Message = "Role not found." };

        var existing = await _dbContext.RoleAssignments
            .FirstOrDefaultAsync(ra => ra.ResourceId == personId && ra.RoleId == roleId, cancellationToken);
        if (existing != null)
            return new ToolResult { Action = "exists", Message = $"**{person.Name}** is already assigned to **{role.Name}**." };

        var assignment = new RoleAssignment
        {
            ResourceId = personId,
            RoleId = roleId,
            AllocationPercentage = allocation,
            IsPrimary = isPrimary
        };

        _dbContext.RoleAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "assigned",
            Message = $"Successfully assigned **{person.Name}** to **{role.Name}** ({allocation}% allocation).",
            Data = JsonSerializer.SerializeToElement(new { assignmentId = assignment.Id })
        };
    }

    private async Task<ToolResult> AddCapabilityAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var personId = Guid.Parse(input.Value.GetProperty("personId").GetString()!);
        var functionId = Guid.Parse(input.Value.GetProperty("functionId").GetString()!);
        var levelStr = input.Value.TryGetProperty("level", out var levelProp) ? levelProp.GetString() : "Capable";

        if (!Enum.TryParse<CapabilityLevel>(levelStr, true, out var level))
            level = CapabilityLevel.Capable;

        var person = await _dbContext.Resources
            .FirstOrDefaultAsync(r => r.Id == personId && r.OrganizationId == organizationId, cancellationToken);
        if (person == null)
            return new ToolResult { Action = "error", Message = "Person not found." };

        var function = await _dbContext.Functions
            .FirstOrDefaultAsync(f => f.Id == functionId && f.OrganizationId == organizationId, cancellationToken);
        if (function == null)
            return new ToolResult { Action = "error", Message = "Function not found." };

        var existing = await _dbContext.FunctionCapabilities
            .FirstOrDefaultAsync(fc => fc.ResourceId == personId && fc.FunctionId == functionId, cancellationToken);
        if (existing != null)
            return new ToolResult { Action = "exists", Message = $"**{person.Name}** already has **{function.Name}** capability." };

        var capability = new FunctionCapability
        {
            ResourceId = personId,
            FunctionId = functionId,
            Level = level
        };

        _dbContext.FunctionCapabilities.Add(capability);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "added",
            Message = $"Added **{function.Name}** capability ({level}) to **{person.Name}**.",
            Data = JsonSerializer.SerializeToElement(new { capabilityId = capability.Id })
        };
    }

    private async Task<ToolResult> AnalyzeCoverageAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Organization Coverage Analysis\n");

        var emptyRoles = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .Where(r => !_dbContext.RoleAssignments.Any(ra => ra.RoleId == r.Id))
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        if (emptyRoles.Any())
        {
            sb.AppendLine("### Uncovered Roles");
            foreach (var role in emptyRoles)
                sb.AppendLine($"- **{role}** - needs assignment");
            sb.AppendLine();
        }

        var spofRoles = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .Where(r => _dbContext.RoleAssignments.Count(ra => ra.RoleId == r.Id) == 1)
            .Select(r => new { r.Name, Person = _dbContext.RoleAssignments.Where(ra => ra.RoleId == r.Id).Select(ra => ra.Resource.Name).FirstOrDefault() })
            .ToListAsync(cancellationToken);

        if (spofRoles.Any())
        {
            sb.AppendLine("### Single Points of Failure");
            foreach (var role in spofRoles)
                sb.AppendLine($"- **{role.Name}** - only {role.Person}");
            sb.AppendLine();
        }

        var uncoveredFunctions = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId)
            .Where(f => !f.FunctionCapabilities.Any())
            .Select(f => f.Name)
            .ToListAsync(cancellationToken);

        if (uncoveredFunctions.Any())
        {
            sb.AppendLine("### Uncovered Functions");
            foreach (var func in uncoveredFunctions)
                sb.AppendLine($"- **{func}** - needs training");
            sb.AppendLine();
        }

        var totalRoles = await _dbContext.Roles.CountAsync(r => r.OrganizationId == organizationId, cancellationToken);
        var totalFunctions = await _dbContext.Functions.CountAsync(f => f.OrganizationId == organizationId, cancellationToken);

        sb.AppendLine("### Summary");
        if (totalRoles > 0)
            sb.AppendLine($"- Role Coverage: {totalRoles - emptyRoles.Count}/{totalRoles}");
        if (totalFunctions > 0)
            sb.AppendLine($"- Function Coverage: {totalFunctions - uncoveredFunctions.Count}/{totalFunctions}");
        sb.AppendLine($"- SPOFs: {spofRoles.Count}");

        if (!emptyRoles.Any() && !spofRoles.Any() && !uncoveredFunctions.Any())
            sb.AppendLine("\nNo critical gaps detected.");

        return new ToolResult { Action = "analyzed", Message = sb.ToString() };
    }

    #region Function Management Tools

    private async Task<ToolResult> CreateFunctionAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var name = input.Value.GetProperty("name").GetString()!;
        var description = input.Value.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
        var purpose = input.Value.TryGetProperty("purpose", out var purposeProp) ? purposeProp.GetString() : null;
        var category = input.Value.TryGetProperty("category", out var catProp) ? catProp.GetString() : null;

        // Check for duplicate name
        var exists = await _dbContext.Functions
            .AnyAsync(f => f.OrganizationId == organizationId && f.Name == name, cancellationToken);
        if (exists)
            return new ToolResult { Action = "exists", Message = $"A function named **{name}** already exists." };

        var func = new Function
        {
            Name = name,
            Description = description,
            Purpose = purpose,
            Category = category,
            Status = FunctionStatus.Active,
            OrganizationId = organizationId
        };

        _dbContext.Functions.Add(func);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "created",
            Message = $"Successfully created function **{name}**" + (category != null ? $" in category '{category}'" : "") + ".",
            Data = JsonSerializer.SerializeToElement(new { id = func.Id, name = func.Name, category = func.Category })
        };
    }

    private async Task<ToolResult> UpdateFunctionAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var functionId = Guid.Parse(input.Value.GetProperty("functionId").GetString()!);
        var func = await _dbContext.Functions
            .FirstOrDefaultAsync(f => f.Id == functionId && f.OrganizationId == organizationId, cancellationToken);

        if (func == null)
            return new ToolResult { Action = "error", Message = "Function not found." };

        if (input.Value.TryGetProperty("name", out var nameProp))
            func.Name = nameProp.GetString()!;

        if (input.Value.TryGetProperty("description", out var descProp))
            func.Description = descProp.GetString();

        if (input.Value.TryGetProperty("purpose", out var purposeProp))
            func.Purpose = purposeProp.GetString();

        if (input.Value.TryGetProperty("category", out var catProp))
            func.Category = catProp.GetString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "updated",
            Message = $"Successfully updated function **{func.Name}**.",
            Data = JsonSerializer.SerializeToElement(new { id = func.Id, name = func.Name, category = func.Category })
        };
    }

    private async Task<ToolResult> BulkCreateFunctionsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var functionsArray = input.Value.GetProperty("functions");
        var created = new List<string>();
        var errors = new List<string>();

        foreach (var funcData in functionsArray.EnumerateArray())
        {
            try
            {
                var name = funcData.GetProperty("name").GetString()!;
                var description = funcData.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
                var purpose = funcData.TryGetProperty("purpose", out var purposeProp) ? purposeProp.GetString() : null;
                var category = funcData.TryGetProperty("category", out var catProp) ? catProp.GetString() : null;

                var exists = await _dbContext.Functions
                    .AnyAsync(f => f.OrganizationId == organizationId && f.Name == name, cancellationToken);
                if (exists)
                {
                    errors.Add($"'{name}' already exists");
                    continue;
                }

                var func = new Function
                {
                    Name = name,
                    Description = description,
                    Purpose = purpose,
                    Category = category,
                    Status = FunctionStatus.Active,
                    OrganizationId = organizationId
                };

                _dbContext.Functions.Add(func);
                await _dbContext.SaveChangesAsync(cancellationToken);
                created.Add(name);
            }
            catch (Exception ex)
            {
                errors.Add($"Error: {ex.Message}");
            }
        }

        var sb = new StringBuilder();
        if (created.Any())
        {
            sb.AppendLine($"Successfully created **{created.Count}** functions:");
            foreach (var name in created)
                sb.AppendLine($"- {name}");
        }
        if (errors.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Issues:");
            foreach (var err in errors)
                sb.AppendLine($"- {err}");
        }

        return new ToolResult
        {
            Action = "bulk_created",
            Message = sb.ToString()
        };
    }

    private async Task<ToolResult> SuggestFunctionsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var context = input?.TryGetProperty("context", out var ctxProp) == true ? ctxProp.GetString() : null;
        var count = input?.TryGetProperty("count", out var cntProp) == true ? cntProp.GetInt32() : 5;

        // Get existing functions to avoid suggesting duplicates
        var existingFunctions = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId)
            .Select(f => f.Name.ToLower())
            .ToListAsync(cancellationToken);

        // Get existing categories
        var existingCategories = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId && f.Category != null)
            .Select(f => f.Category!)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Get roles and processes for context
        var roles = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        var processes = await _dbContext.Processes
            .Where(p => p.OrganizationId == organizationId)
            .Select(p => p.Name)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Function Suggestions\n");
        sb.AppendLine($"Based on your organization's roles ({string.Join(", ", roles.Take(5))}{(roles.Count > 5 ? "..." : "")})");
        if (processes.Any())
            sb.AppendLine($"and processes ({string.Join(", ", processes.Take(5))}{(processes.Count > 5 ? "..." : "")}), ");
        sb.AppendLine($"here are suggested functions{(context != null ? $" focused on '{context}'" : "")}:\n");
        sb.AppendLine("**Note**: These are suggestions for you to review. Ask me to create any that seem useful.");
        sb.AppendLine();
        sb.AppendLine("| Function | Category | Purpose |");
        sb.AppendLine("|----------|----------|---------|");

        // Generate suggestions based on context and existing data
        var suggestions = GenerateFunctionSuggestions(context, existingFunctions, existingCategories, roles, processes, count);
        foreach (var suggestion in suggestions)
        {
            sb.AppendLine($"| {suggestion.Name} | {suggestion.Category ?? "General"} | {suggestion.Purpose ?? "-"} |");
        }

        sb.AppendLine();
        sb.AppendLine("Would you like me to create any of these functions?");

        return new ToolResult { Action = "suggested", Message = sb.ToString() };
    }

    private List<(string Name, string? Category, string? Purpose)> GenerateFunctionSuggestions(
        string? context,
        List<string> existing,
        List<string> categories,
        List<string> roles,
        List<string> processes,
        int count)
    {
        var suggestions = new List<(string Name, string? Category, string? Purpose)>();

        // Common business functions by context
        var commonFunctions = new Dictionary<string, List<(string Name, string Category, string Purpose)>>
        {
            ["technical"] = new()
            {
                ("Code Review", "Technical", "Ensure code quality and knowledge sharing"),
                ("Technical Documentation", "Technical", "Document systems and architecture"),
                ("System Architecture", "Technical", "Design scalable system solutions"),
                ("Database Administration", "Technical", "Manage and optimize databases"),
                ("DevOps Pipeline Management", "Technical", "Maintain CI/CD processes"),
                ("Security Audit", "Technical", "Assess and improve security posture"),
                ("Performance Optimization", "Technical", "Improve system performance"),
                ("Technical Support Escalation", "Technical", "Handle complex technical issues"),
            },
            ["consulting"] = new()
            {
                ("Business Analysis", "Consulting", "Analyze client business needs"),
                ("Solution Design", "Consulting", "Design tailored solutions"),
                ("Stakeholder Management", "Consulting", "Coordinate with key stakeholders"),
                ("Change Management", "Consulting", "Guide organizational change"),
                ("Workshop Facilitation", "Consulting", "Run discovery and planning workshops"),
                ("ROI Analysis", "Consulting", "Calculate return on investment"),
                ("Gap Analysis", "Consulting", "Identify gaps between current and target state"),
                ("Risk Assessment", "Consulting", "Evaluate and mitigate project risks"),
            },
            ["support"] = new()
            {
                ("Incident Management", "Support", "Handle and resolve incidents"),
                ("Customer Escalation", "Support", "Handle escalated customer issues"),
                ("Knowledge Base Management", "Support", "Maintain support documentation"),
                ("SLA Monitoring", "Support", "Track and ensure SLA compliance"),
                ("Root Cause Analysis", "Support", "Identify underlying issue causes"),
                ("User Training", "Support", "Train users on systems"),
                ("Feedback Collection", "Support", "Gather and analyze customer feedback"),
            },
            ["sales"] = new()
            {
                ("Lead Qualification", "Sales", "Assess and qualify sales leads"),
                ("Proposal Writing", "Sales", "Create compelling proposals"),
                ("Demo Delivery", "Sales", "Present product demonstrations"),
                ("Contract Negotiation", "Sales", "Negotiate terms and pricing"),
                ("Account Planning", "Sales", "Develop strategic account plans"),
                ("Competitive Analysis", "Sales", "Analyze competitor offerings"),
            },
            ["general"] = new()
            {
                ("Project Management", "Operations", "Plan and execute projects"),
                ("Resource Planning", "Operations", "Allocate and manage resources"),
                ("Quality Assurance", "Operations", "Ensure deliverable quality"),
                ("Process Improvement", "Operations", "Optimize business processes"),
                ("Vendor Management", "Operations", "Manage external vendors"),
                ("Compliance Monitoring", "Operations", "Ensure regulatory compliance"),
                ("Budget Management", "Finance", "Manage budgets and costs"),
                ("Reporting & Analytics", "Operations", "Generate insights and reports"),
            }
        };

        // Determine which function sets to use
        var contextLower = context?.ToLower() ?? "general";
        var relevantFunctions = commonFunctions.GetValueOrDefault(contextLower, commonFunctions["general"]);

        // Also add general functions
        if (contextLower != "general")
            relevantFunctions.AddRange(commonFunctions["general"]);

        // Filter out existing functions and take requested count
        foreach (var func in relevantFunctions)
        {
            if (!existing.Contains(func.Name.ToLower()) && suggestions.Count < count)
            {
                suggestions.Add((func.Name, func.Category, func.Purpose));
            }
        }

        return suggestions;
    }

    private async Task<ToolResult> SuggestImprovementsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var functions = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        if (!functions.Any())
            return new ToolResult { Action = "no_data", Message = "No functions exist yet. Create some functions first!" };

        var sb = new StringBuilder();
        sb.AppendLine("## Function Improvement Suggestions\n");

        var improvements = new List<string>();

        // Check for missing descriptions
        var noDescription = functions.Where(f => string.IsNullOrWhiteSpace(f.Description)).ToList();
        if (noDescription.Any())
        {
            sb.AppendLine("### Functions Missing Descriptions");
            foreach (var func in noDescription.Take(5))
                sb.AppendLine($"- **{func.Name}**");
            if (noDescription.Count > 5)
                sb.AppendLine($"  ...and {noDescription.Count - 5} more");
            sb.AppendLine();
            improvements.Add($"{noDescription.Count} functions need descriptions");
        }

        // Check for missing categories
        var noCategory = functions.Where(f => string.IsNullOrWhiteSpace(f.Category)).ToList();
        if (noCategory.Any())
        {
            sb.AppendLine("### Functions Missing Categories");
            foreach (var func in noCategory.Take(5))
                sb.AppendLine($"- **{func.Name}**");
            if (noCategory.Count > 5)
                sb.AppendLine($"  ...and {noCategory.Count - 5} more");
            sb.AppendLine();
            improvements.Add($"{noCategory.Count} functions need categories");
        }

        // Check for potential duplicates (similar names)
        var potentialDuplicates = functions
            .SelectMany(f1 => functions
                .Where(f2 => f1.Id != f2.Id && AreSimilarNames(f1.Name, f2.Name))
                .Select(f2 => (f1.Name, f2.Name)))
            .Distinct()
            .Take(5)
            .ToList();

        if (potentialDuplicates.Any())
        {
            sb.AppendLine("### Potential Duplicate Functions");
            foreach (var (name1, name2) in potentialDuplicates)
                sb.AppendLine($"- **{name1}** and **{name2}** - consider consolidating");
            sb.AppendLine();
            improvements.Add($"{potentialDuplicates.Count} potential duplicates found");
        }

        // Check category distribution
        var categoryGroups = functions
            .Where(f => !string.IsNullOrWhiteSpace(f.Category))
            .GroupBy(f => f.Category)
            .OrderByDescending(g => g.Count())
            .ToList();

        if (categoryGroups.Any())
        {
            sb.AppendLine("### Category Distribution");
            foreach (var group in categoryGroups)
                sb.AppendLine($"- **{group.Key}**: {group.Count()} functions");
            sb.AppendLine();
        }

        // Check for uncovered functions (no one has capability)
        var uncoveredFunctions = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId && !f.FunctionCapabilities.Any())
            .Select(f => f.Name)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (uncoveredFunctions.Any())
        {
            sb.AppendLine("### Uncovered Functions (No One Assigned)");
            foreach (var name in uncoveredFunctions)
                sb.AppendLine($"- **{name}**");
            sb.AppendLine();
            improvements.Add($"{uncoveredFunctions.Count}+ functions have no assigned capabilities");
        }

        if (!improvements.Any())
        {
            sb.AppendLine("Your functions are well-organized! No major improvements needed.");
        }
        else
        {
            sb.AppendLine("### Summary");
            foreach (var imp in improvements)
                sb.AppendLine($"- {imp}");
            sb.AppendLine();
            sb.AppendLine("Would you like me to help fix any of these issues?");
        }

        return new ToolResult { Action = "analyzed", Message = sb.ToString() };
    }

    private bool AreSimilarNames(string name1, string name2)
    {
        // Simple similarity check - could be improved with Levenshtein distance
        var words1 = name1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var words2 = name2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Check if significant words overlap
        var overlap = words1.Intersect(words2).Count();
        return overlap >= Math.Min(words1.Length, words2.Length) * 0.5 && overlap > 0;
    }

    private async Task<ToolResult> DeleteFunctionAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var functionId = Guid.Parse(input.Value.GetProperty("functionId").GetString()!);
        var func = await _dbContext.Functions
            .Include(f => f.FunctionCapabilities)
            .FirstOrDefaultAsync(f => f.Id == functionId && f.OrganizationId == organizationId, cancellationToken);

        if (func == null)
            return new ToolResult { Action = "error", Message = "Function not found." };

        var name = func.Name;
        var capabilityCount = func.FunctionCapabilities.Count;

        if (capabilityCount > 0)
        {
            return new ToolResult
            {
                Action = "warning",
                Message = $"**{name}** has {capabilityCount} people with this capability. Deleting it will remove those capability assignments. Please confirm you want to proceed."
            };
        }

        // Soft delete - CLAUDE.md compliance
        func.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "deleted",
            Message = $"Successfully deleted function **{name}**."
        };
    }

    #endregion

    #region Process Management Tools

    private async Task<ToolResult> CreateProcessAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var name = input.Value.GetProperty("name").GetString()!;
        var purpose = input.Value.TryGetProperty("purpose", out var purposeProp) ? purposeProp.GetString() : null;
        var description = input.Value.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
        var trigger = input.Value.TryGetProperty("trigger", out var triggerProp) ? triggerProp.GetString() : null;
        var output = input.Value.TryGetProperty("output", out var outputProp) ? outputProp.GetString() : null;
        var frequencyStr = input.Value.TryGetProperty("frequency", out var freqProp) ? freqProp.GetString() : "OnDemand";

        if (!Enum.TryParse<ProcessFrequency>(frequencyStr, true, out var frequency))
            frequency = ProcessFrequency.OnDemand;

        var process = new Process
        {
            Name = name,
            Purpose = purpose,
            Description = description,
            Trigger = trigger,
            Output = output,
            Frequency = frequency,
            Status = ProcessStatus.Active,
            StateType = ProcessStateType.Current,
            OrganizationId = organizationId
        };

        _dbContext.Processes.Add(process);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "created",
            Message = $"Successfully created process **{name}**.",
            Data = JsonSerializer.SerializeToElement(new { id = process.Id, name = process.Name })
        };
    }

    private async Task<ToolResult> BulkCreateProcessesAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var processesArray = input.Value.GetProperty("processes");
        var created = new List<string>();

        foreach (var procData in processesArray.EnumerateArray())
        {
            var name = procData.GetProperty("name").GetString()!;
            var purpose = procData.TryGetProperty("purpose", out var purposeProp) ? purposeProp.GetString() : null;
            var description = procData.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
            var frequencyStr = procData.TryGetProperty("frequency", out var freqProp) ? freqProp.GetString() : "OnDemand";

            if (!Enum.TryParse<ProcessFrequency>(frequencyStr, true, out var frequency))
                frequency = ProcessFrequency.OnDemand;

            var process = new Process
            {
                Name = name,
                Purpose = purpose,
                Description = description,
                Frequency = frequency,
                Status = ProcessStatus.Active,
                StateType = ProcessStateType.Current,
                OrganizationId = organizationId
            };

            _dbContext.Processes.Add(process);
            created.Add(name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ToolResult { Action = "bulk_created", Message = $"Successfully created {created.Count} processes: {string.Join(", ", created)}" };
    }

    private async Task<ToolResult> AddActivitiesToProcessAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var processIdStr = input.Value.GetProperty("processId").GetString()!;
        if (!Guid.TryParse(processIdStr, out var processId))
            return new ToolResult { Action = "error", Message = $"Invalid process ID: {processIdStr}" };

        // Verify the process exists and belongs to this organization
        var process = await _dbContext.Processes
            .FirstOrDefaultAsync(p => p.Id == processId && p.OrganizationId == organizationId, cancellationToken);

        if (process == null)
            return new ToolResult { Action = "error", Message = $"Process not found with ID: {processIdStr}" };

        var activitiesArray = input.Value.GetProperty("activities");
        var created = new List<string>();
        var currentOrder = await _dbContext.Activities
            .Where(a => a.ProcessId == processId)
            .MaxAsync(a => (int?)a.Order, cancellationToken) ?? 0;

        foreach (var actData in activitiesArray.EnumerateArray())
        {
            var name = actData.GetProperty("name").GetString()!;
            var description = actData.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
            var instructions = actData.TryGetProperty("instructions", out var instrProp) ? instrProp.GetString() : null;
            var activityTypeStr = actData.TryGetProperty("activityType", out var typeProp) ? typeProp.GetString() : "Manual";
            var estimatedDuration = actData.TryGetProperty("estimatedDurationMinutes", out var durProp) ? (int?)durProp.GetInt32() : null;
            var orderVal = actData.TryGetProperty("order", out var orderProp) ? orderProp.GetInt32() : ++currentOrder;

            if (!Enum.TryParse<ActivityType>(activityTypeStr, true, out var activityType))
                activityType = ActivityType.Manual;

            var activity = new Activity
            {
                Name = name,
                Description = description,
                Instructions = instructions,
                ActivityType = activityType,
                EstimatedDurationMinutes = estimatedDuration,
                Order = orderVal,
                ProcessId = processId,
                PositionX = 100 + (created.Count * 200), // Space out activities horizontally
                PositionY = 200
            };

            _dbContext.Activities.Add(activity);
            created.Add(name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"Successfully added {created.Count} activities to process **{process.Name}**:");
        foreach (var actName in created)
        {
            sb.AppendLine($"- {actName}");
        }
        sb.AppendLine();
        sb.AppendLine("The activities have been added in sequence. You can view and edit them in the process editor.");

        return new ToolResult
        {
            Action = "activities_added",
            Message = sb.ToString(),
            Data = JsonSerializer.SerializeToElement(new { processId, processName = process.Name, activitiesAdded = created.Count, activityNames = created })
        };
    }

    #endregion

    #region Goal Management Tools

    private async Task<ToolResult> CreateGoalAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var name = input.Value.GetProperty("name").GetString()!;
        var goalTypeStr = input.Value.GetProperty("goalType").GetString()!;
        var description = input.Value.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
        var targetValue = input.Value.TryGetProperty("targetValue", out var tvProp) ? (decimal?)tvProp.GetDecimal() : null;
        var unit = input.Value.TryGetProperty("unit", out var unitProp) ? unitProp.GetString() : null;
        var parentId = input.Value.TryGetProperty("parentId", out var pidProp) ? Guid.Parse(pidProp.GetString()!) : (Guid?)null;

        if (!Enum.TryParse<GoalType>(goalTypeStr, true, out var goalType))
            goalType = GoalType.Objective;

        var goal = new Goal
        {
            Name = name,
            Description = description,
            GoalType = goalType,
            TargetValue = targetValue,
            Unit = unit,
            ParentId = parentId,
            Status = GoalStatus.Active,
            OrganizationId = organizationId
        };

        _dbContext.Goals.Add(goal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "created",
            Message = $"Successfully created {goalType} **{name}**.",
            Data = JsonSerializer.SerializeToElement(new { id = goal.Id, name = goal.Name, type = goal.GoalType.ToString() })
        };
    }

    private async Task<ToolResult> BulkCreateGoalsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var goalsArray = input.Value.GetProperty("goals");
        var created = new List<string>();

        foreach (var goalData in goalsArray.EnumerateArray())
        {
            var name = goalData.GetProperty("name").GetString()!;
            var goalTypeStr = goalData.TryGetProperty("goalType", out var gtProp) ? gtProp.GetString() : "Objective";

            if (!Enum.TryParse<GoalType>(goalTypeStr, true, out var goalType))
                goalType = GoalType.Objective;

            var goal = new Goal
            {
                Name = name,
                Description = goalData.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
                GoalType = goalType,
                Status = GoalStatus.Active,
                OrganizationId = organizationId
            };

            _dbContext.Goals.Add(goal);
            created.Add(name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ToolResult { Action = "bulk_created", Message = $"Successfully created {created.Count} goals: {string.Join(", ", created)}" };
    }

    #endregion

    #region Partner Management Tools

    private async Task<ToolResult> CreatePartnerAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var name = input.Value.GetProperty("name").GetString()!;
        var typeStr = input.Value.GetProperty("type").GetString()!;
        var description = input.Value.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
        var strategicValueStr = input.Value.TryGetProperty("strategicValue", out var svProp) ? svProp.GetString() : "Medium";

        if (!Enum.TryParse<PartnerType>(typeStr, true, out var partnerType))
            partnerType = PartnerType.Strategic;

        if (!Enum.TryParse<StrategicValue>(strategicValueStr, true, out var strategicValue))
            strategicValue = StrategicValue.Medium;

        var partner = new Partner
        {
            Name = name,
            Description = description,
            Type = partnerType,
            StrategicValue = strategicValue,
            Status = PartnerStatus.Active,
            OrganizationId = organizationId
        };

        _dbContext.Partners.Add(partner);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "created",
            Message = $"Successfully created partner **{name}** ({partnerType}).",
            Data = JsonSerializer.SerializeToElement(new { id = partner.Id, name = partner.Name })
        };
    }

    private async Task<ToolResult> BulkCreatePartnersAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var partnersArray = input.Value.GetProperty("partners");
        var created = new List<string>();

        foreach (var partnerData in partnersArray.EnumerateArray())
        {
            var name = partnerData.GetProperty("name").GetString()!;
            var typeStr = partnerData.TryGetProperty("type", out var tProp) ? tProp.GetString() : "Strategic";

            if (!Enum.TryParse<PartnerType>(typeStr, true, out var partnerType))
                partnerType = PartnerType.Strategic;

            var partner = new Partner
            {
                Name = name,
                Description = partnerData.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
                Type = partnerType,
                StrategicValue = StrategicValue.Medium,
                Status = PartnerStatus.Active,
                OrganizationId = organizationId
            };

            _dbContext.Partners.Add(partner);
            created.Add(name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ToolResult { Action = "bulk_created", Message = $"Successfully created {created.Count} partners: {string.Join(", ", created)}" };
    }

    #endregion

    #region Channel Management Tools

    private async Task<ToolResult> CreateChannelAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var name = input.Value.GetProperty("name").GetString()!;
        var typeStr = input.Value.GetProperty("type").GetString()!;
        var categoryStr = input.Value.GetProperty("category").GetString()!;
        var description = input.Value.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
        var ownershipStr = input.Value.TryGetProperty("ownership", out var ownProp) ? ownProp.GetString() : "Owned";

        if (!Enum.TryParse<ChannelType>(typeStr, true, out var channelType))
            channelType = ChannelType.Digital;

        if (!Enum.TryParse<ChannelCategory>(categoryStr, true, out var category))
            category = ChannelCategory.Sales;

        if (!Enum.TryParse<ChannelOwnership>(ownershipStr, true, out var ownership))
            ownership = ChannelOwnership.Owned;

        var channel = new Channel
        {
            Name = name,
            Description = description,
            Type = channelType,
            Category = category,
            Ownership = ownership,
            Status = ChannelStatus.Active,
            OrganizationId = organizationId
        };

        _dbContext.Channels.Add(channel);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "created",
            Message = $"Successfully created channel **{name}** ({channelType}, {category}).",
            Data = JsonSerializer.SerializeToElement(new { id = channel.Id, name = channel.Name })
        };
    }

    private async Task<ToolResult> BulkCreateChannelsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var channelsArray = input.Value.GetProperty("channels");
        var created = new List<string>();

        foreach (var channelData in channelsArray.EnumerateArray())
        {
            var name = channelData.GetProperty("name").GetString()!;
            var typeStr = channelData.TryGetProperty("type", out var tProp) ? tProp.GetString() : "Digital";
            var categoryStr = channelData.TryGetProperty("category", out var cProp) ? cProp.GetString() : "Sales";

            if (!Enum.TryParse<ChannelType>(typeStr, true, out var channelType))
                channelType = ChannelType.Digital;

            if (!Enum.TryParse<ChannelCategory>(categoryStr, true, out var category))
                category = ChannelCategory.Sales;

            var channel = new Channel
            {
                Name = name,
                Description = channelData.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
                Type = channelType,
                Category = category,
                Ownership = ChannelOwnership.Owned,
                Status = ChannelStatus.Active,
                OrganizationId = organizationId
            };

            _dbContext.Channels.Add(channel);
            created.Add(name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ToolResult { Action = "bulk_created", Message = $"Successfully created {created.Count} channels: {string.Join(", ", created)}" };
    }

    #endregion

    #region Value Proposition Management Tools

    private async Task<ToolResult> CreateValuePropositionAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var name = input.Value.GetProperty("name").GetString()!;
        var headline = input.Value.GetProperty("headline").GetString()!;
        var description = input.Value.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

        var vp = new ValueProposition
        {
            Name = name,
            Headline = headline,
            Description = description,
            Status = ValuePropositionStatus.Active,
            OrganizationId = organizationId
        };

        _dbContext.ValuePropositions.Add(vp);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "created",
            Message = $"Successfully created value proposition **{name}**.",
            Data = JsonSerializer.SerializeToElement(new { id = vp.Id, name = vp.Name })
        };
    }

    private async Task<ToolResult> BulkCreateValuePropositionsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var vpArray = input.Value.GetProperty("valuePropositions");
        var created = new List<string>();

        foreach (var vpData in vpArray.EnumerateArray())
        {
            var name = vpData.GetProperty("name").GetString()!;
            var headline = vpData.TryGetProperty("headline", out var hProp) ? hProp.GetString() : name;

            var vp = new ValueProposition
            {
                Name = name,
                Headline = headline ?? name,
                Description = vpData.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
                Status = ValuePropositionStatus.Active,
                OrganizationId = organizationId
            };

            _dbContext.ValuePropositions.Add(vp);
            created.Add(name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ToolResult { Action = "bulk_created", Message = $"Successfully created {created.Count} value propositions: {string.Join(", ", created)}" };
    }

    #endregion

    #region Customer Relationship Management Tools

    private async Task<ToolResult> CreateCustomerRelationshipAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var name = input.Value.GetProperty("name").GetString()!;
        var typeStr = input.Value.GetProperty("type").GetString()!;
        var description = input.Value.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

        if (!Enum.TryParse<CustomerRelationshipType>(typeStr, true, out var crType))
            crType = CustomerRelationshipType.PersonalAssistance;

        var cr = new CustomerRelationship
        {
            Name = name,
            Description = description,
            Type = crType,
            Status = CustomerRelationshipStatus.Active,
            OrganizationId = organizationId
        };

        _dbContext.CustomerRelationships.Add(cr);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "created",
            Message = $"Successfully created customer relationship **{name}** ({crType}).",
            Data = JsonSerializer.SerializeToElement(new { id = cr.Id, name = cr.Name })
        };
    }

    private async Task<ToolResult> BulkCreateCustomerRelationshipsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var crArray = input.Value.GetProperty("customerRelationships");
        var created = new List<string>();

        foreach (var crData in crArray.EnumerateArray())
        {
            var name = crData.GetProperty("name").GetString()!;
            var typeStr = crData.TryGetProperty("type", out var tProp) ? tProp.GetString() : "PersonalAssistance";

            if (!Enum.TryParse<CustomerRelationshipType>(typeStr, true, out var crType))
                crType = CustomerRelationshipType.PersonalAssistance;

            var cr = new CustomerRelationship
            {
                Name = name,
                Description = crData.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
                Type = crType,
                Status = CustomerRelationshipStatus.Active,
                OrganizationId = organizationId
            };

            _dbContext.CustomerRelationships.Add(cr);
            created.Add(name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ToolResult { Action = "bulk_created", Message = $"Successfully created {created.Count} customer relationships: {string.Join(", ", created)}" };
    }

    #endregion

    #region Revenue Stream Management Tools

    private async Task<ToolResult> CreateRevenueStreamAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var name = input.Value.GetProperty("name").GetString()!;
        var typeStr = input.Value.GetProperty("type").GetString()!;
        var description = input.Value.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
        var pricingStr = input.Value.TryGetProperty("pricingMechanism", out var pProp) ? pProp.GetString() : "Fixed";

        if (!Enum.TryParse<RevenueStreamType>(typeStr, true, out var rsType))
            rsType = RevenueStreamType.AssetSale;

        if (!Enum.TryParse<PricingMechanism>(pricingStr, true, out var pricing))
            pricing = PricingMechanism.Fixed;

        var rs = new RevenueStream
        {
            Name = name,
            Description = description,
            Type = rsType,
            PricingMechanism = pricing,
            Status = RevenueStreamStatus.Active,
            OrganizationId = organizationId
        };

        _dbContext.RevenueStreams.Add(rs);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "created",
            Message = $"Successfully created revenue stream **{name}** ({rsType}).",
            Data = JsonSerializer.SerializeToElement(new { id = rs.Id, name = rs.Name })
        };
    }

    private async Task<ToolResult> BulkCreateRevenueStreamsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var rsArray = input.Value.GetProperty("revenueStreams");
        var created = new List<string>();

        foreach (var rsData in rsArray.EnumerateArray())
        {
            var name = rsData.GetProperty("name").GetString()!;
            var typeStr = rsData.TryGetProperty("type", out var tProp) ? tProp.GetString() : "AssetSale";

            if (!Enum.TryParse<RevenueStreamType>(typeStr, true, out var rsType))
                rsType = RevenueStreamType.AssetSale;

            var rs = new RevenueStream
            {
                Name = name,
                Description = rsData.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
                Type = rsType,
                PricingMechanism = PricingMechanism.Fixed,
                Status = RevenueStreamStatus.Active,
                OrganizationId = organizationId
            };

            _dbContext.RevenueStreams.Add(rs);
            created.Add(name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ToolResult { Action = "bulk_created", Message = $"Successfully created {created.Count} revenue streams: {string.Join(", ", created)}" };
    }

    #endregion

    #region Role Management Tools

    private async Task<ToolResult> CreateRoleAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var name = input.Value.GetProperty("name").GetString()!;
        var description = input.Value.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
        var department = input.Value.TryGetProperty("department", out var deptProp) ? deptProp.GetString() : null;

        var role = new Role
        {
            Name = name,
            Description = description,
            Department = department,
            OrganizationId = organizationId
        };

        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "created",
            Message = $"Successfully created role **{name}**" + (department != null ? $" in {department}" : "") + ".",
            Data = JsonSerializer.SerializeToElement(new { id = role.Id, name = role.Name })
        };
    }

    private async Task<ToolResult> BulkCreateRolesAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var rolesArray = input.Value.GetProperty("roles");
        var created = new List<string>();

        foreach (var roleData in rolesArray.EnumerateArray())
        {
            var name = roleData.GetProperty("name").GetString()!;

            var role = new Role
            {
                Name = name,
                Description = roleData.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
                Department = roleData.TryGetProperty("department", out var deptProp) ? deptProp.GetString() : null,
                OrganizationId = organizationId
            };

            _dbContext.Roles.Add(role);
            created.Add(name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ToolResult { Action = "bulk_created", Message = $"Successfully created {created.Count} roles: {string.Join(", ", created)}" };
    }

    #region AI Agent Management Tools

    private async Task<ToolResult> CreateAiAgentAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var name = input.Value.GetProperty("name").GetString()!;
        var roleTitle = input.Value.GetProperty("roleTitle").GetString()!;
        var systemPrompt = input.Value.GetProperty("systemPrompt").GetString()!;

        // Check for duplicate name
        var existingAgent = await _dbContext.AiAgents
            .FirstOrDefaultAsync(a => a.Name == name && a.OrganizationId == organizationId && a.DeletedAt == null, cancellationToken);

        if (existingAgent != null)
            return new ToolResult { Action = "error", Message = $"An AI agent named '{name}' already exists." };

        // Parse provider
        var providerStr = input.Value.TryGetProperty("provider", out var provProp) ? provProp.GetString() : "Anthropic";
        var provider = providerStr?.ToLower() switch
        {
            "openai" => AiProvider.OpenAI,
            "google" => AiProvider.Google,
            _ => AiProvider.Anthropic
        };

        // Parse communication style
        var commStyleStr = input.Value.TryGetProperty("communicationStyle", out var commProp) ? commProp.GetString() : "Formal";
        var communicationStyle = commStyleStr?.ToLower() switch
        {
            "casual" => CommunicationStyle.Casual,
            "direct" => CommunicationStyle.Direct,
            "diplomatic" => CommunicationStyle.Diplomatic,
            "analytical" => CommunicationStyle.Analytical,
            _ => CommunicationStyle.Formal
        };

        // Parse reaction tendency
        var reactionStr = input.Value.TryGetProperty("reactionTendency", out var reactProp) ? reactProp.GetString() : "Balanced";
        var reactionTendency = reactionStr?.ToLower() switch
        {
            "supportive" => ReactionTendency.Supportive,
            "critical" => ReactionTendency.Critical,
            "devilsadvocate" => ReactionTendency.DevilsAdvocate,
            "consensusbuilder" => ReactionTendency.ConsensusBuilder,
            _ => ReactionTendency.Balanced
        };

        var agent = new AiAgent
        {
            Name = name,
            RoleTitle = roleTitle,
            SystemPrompt = systemPrompt,
            Provider = provider,
            ModelId = input.Value.TryGetProperty("modelId", out var modelProp) ? modelProp.GetString() ?? "claude-sonnet-4-20250514" : "claude-sonnet-4-20250514",
            ModelDisplayName = input.Value.TryGetProperty("modelDisplayName", out var dispProp) ? dispProp.GetString() ?? "Claude Sonnet" : "Claude Sonnet",
            AvatarColor = input.Value.TryGetProperty("avatarColor", out var colorProp) ? colorProp.GetString() : "#4F46E5",
            MaxTokensPerResponse = input.Value.TryGetProperty("maxTokensPerResponse", out var tokensProp) ? tokensProp.GetInt32() : 4096,
            Temperature = input.Value.TryGetProperty("temperature", out var tempProp) ? tempProp.GetDecimal() : 0.7m,
            Assertiveness = input.Value.TryGetProperty("assertiveness", out var assertProp) ? assertProp.GetInt32() : 50,
            CommunicationStyle = communicationStyle,
            ReactionTendency = reactionTendency,
            ExpertiseAreas = input.Value.TryGetProperty("expertiseAreas", out var expertProp) ? expertProp.GetString() : null,
            SeniorityLevel = input.Value.TryGetProperty("seniorityLevel", out var seniorProp) ? seniorProp.GetInt32() : 3,
            AsksQuestions = input.Value.TryGetProperty("asksQuestions", out var asksProp) && asksProp.GetBoolean(),
            GivesBriefAcknowledgments = !input.Value.TryGetProperty("givesBriefAcknowledgments", out var ackProp) || ackProp.GetBoolean(),
            IsActive = !input.Value.TryGetProperty("isActive", out var activeProp) || activeProp.GetBoolean(),
            OrganizationId = organizationId
        };

        _dbContext.AiAgents.Add(agent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "created",
            Message = $"Successfully created AI agent **{name}** ({roleTitle}) with {communicationStyle} communication style and {reactionTendency} reaction tendency.",
            Data = JsonSerializer.SerializeToElement(new { id = agent.Id, name = agent.Name, roleTitle = agent.RoleTitle })
        };
    }

    private async Task<ToolResult> UpdateAiAgentAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var agentId = Guid.Parse(input.Value.GetProperty("agentId").GetString()!);

        var agent = await _dbContext.AiAgents
            .FirstOrDefaultAsync(a => a.Id == agentId && a.OrganizationId == organizationId && a.DeletedAt == null, cancellationToken);

        if (agent == null)
            return new ToolResult { Action = "error", Message = "AI agent not found." };

        // Update fields if provided
        if (input.Value.TryGetProperty("name", out var nameProp))
            agent.Name = nameProp.GetString()!;
        if (input.Value.TryGetProperty("roleTitle", out var roleProp))
            agent.RoleTitle = roleProp.GetString()!;
        if (input.Value.TryGetProperty("systemPrompt", out var promptProp))
            agent.SystemPrompt = promptProp.GetString()!;
        if (input.Value.TryGetProperty("avatarColor", out var colorProp))
            agent.AvatarColor = colorProp.GetString();
        if (input.Value.TryGetProperty("maxTokensPerResponse", out var tokensProp))
            agent.MaxTokensPerResponse = tokensProp.GetInt32();
        if (input.Value.TryGetProperty("temperature", out var tempProp))
            agent.Temperature = tempProp.GetDecimal();
        if (input.Value.TryGetProperty("assertiveness", out var assertProp))
            agent.Assertiveness = assertProp.GetInt32();
        if (input.Value.TryGetProperty("expertiseAreas", out var expertProp))
            agent.ExpertiseAreas = expertProp.GetString();
        if (input.Value.TryGetProperty("seniorityLevel", out var seniorProp))
            agent.SeniorityLevel = seniorProp.GetInt32();
        if (input.Value.TryGetProperty("asksQuestions", out var asksProp))
            agent.AsksQuestions = asksProp.GetBoolean();
        if (input.Value.TryGetProperty("givesBriefAcknowledgments", out var ackProp))
            agent.GivesBriefAcknowledgments = ackProp.GetBoolean();
        if (input.Value.TryGetProperty("isActive", out var activeProp))
            agent.IsActive = activeProp.GetBoolean();

        if (input.Value.TryGetProperty("provider", out var provProp))
        {
            agent.Provider = provProp.GetString()?.ToLower() switch
            {
                "openai" => AiProvider.OpenAI,
                "google" => AiProvider.Google,
                _ => AiProvider.Anthropic
            };
        }
        if (input.Value.TryGetProperty("modelId", out var modelProp))
            agent.ModelId = modelProp.GetString()!;
        if (input.Value.TryGetProperty("modelDisplayName", out var dispProp))
            agent.ModelDisplayName = dispProp.GetString()!;
        if (input.Value.TryGetProperty("communicationStyle", out var commProp))
        {
            agent.CommunicationStyle = commProp.GetString()?.ToLower() switch
            {
                "casual" => CommunicationStyle.Casual,
                "direct" => CommunicationStyle.Direct,
                "diplomatic" => CommunicationStyle.Diplomatic,
                "analytical" => CommunicationStyle.Analytical,
                _ => CommunicationStyle.Formal
            };
        }
        if (input.Value.TryGetProperty("reactionTendency", out var reactProp))
        {
            agent.ReactionTendency = reactProp.GetString()?.ToLower() switch
            {
                "supportive" => ReactionTendency.Supportive,
                "critical" => ReactionTendency.Critical,
                "devilsadvocate" => ReactionTendency.DevilsAdvocate,
                "consensusbuilder" => ReactionTendency.ConsensusBuilder,
                _ => ReactionTendency.Balanced
            };
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "updated",
            Message = $"Successfully updated AI agent **{agent.Name}**.",
            Data = JsonSerializer.SerializeToElement(new { id = agent.Id, name = agent.Name })
        };
    }

    private async Task<ToolResult> DeleteAiAgentAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var agentId = Guid.Parse(input.Value.GetProperty("agentId").GetString()!);

        var agent = await _dbContext.AiAgents
            .FirstOrDefaultAsync(a => a.Id == agentId && a.OrganizationId == organizationId && a.DeletedAt == null, cancellationToken);

        if (agent == null)
            return new ToolResult { Action = "error", Message = "AI agent not found." };

        var agentName = agent.Name;
        agent.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "deleted",
            Message = $"Successfully deleted AI agent **{agentName}**."
        };
    }

    private async Task<ToolResult> ListAiAgentsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var activeOnly = input?.TryGetProperty("activeOnly", out var activeProp) == true && activeProp.GetBoolean();

        var query = _dbContext.AiAgents
            .Where(a => a.OrganizationId == organizationId && a.DeletedAt == null);

        if (activeOnly)
            query = query.Where(a => a.IsActive);

        var agents = await query
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.RoleTitle,
                a.Provider,
                a.ModelDisplayName,
                a.IsActive,
                a.Assertiveness,
                CommunicationStyle = a.CommunicationStyle.ToString(),
                ReactionTendency = a.ReactionTendency.ToString(),
                a.ExpertiseAreas
            })
            .ToListAsync(cancellationToken);

        var agentList = string.Join("\n", agents.Select(a =>
            $"- **{a.Name}** ({a.RoleTitle}) - {a.ModelDisplayName}, {a.CommunicationStyle}, {(a.IsActive ? "Active" : "Inactive")}"));

        return new ToolResult
        {
            Action = "listed",
            Message = agents.Count == 0
                ? "No AI agents found in the organization."
                : $"Found {agents.Count} AI agents:\n{agentList}",
            Data = JsonSerializer.SerializeToElement(agents)
        };
    }

    private async Task<ToolResult> BulkCreateAiAgentsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var agentsArray = input.Value.GetProperty("agents");
        var created = new List<string>();
        var errors = new List<string>();

        foreach (var agentData in agentsArray.EnumerateArray())
        {
            var name = agentData.GetProperty("name").GetString()!;
            var roleTitle = agentData.GetProperty("roleTitle").GetString()!;
            var systemPrompt = agentData.GetProperty("systemPrompt").GetString()!;

            // Check for duplicate
            var exists = await _dbContext.AiAgents
                .AnyAsync(a => a.Name == name && a.OrganizationId == organizationId && a.DeletedAt == null, cancellationToken);

            if (exists)
            {
                errors.Add($"{name} (already exists)");
                continue;
            }

            var providerStr = agentData.TryGetProperty("provider", out var provProp) ? provProp.GetString() : "Anthropic";
            var provider = providerStr?.ToLower() switch
            {
                "openai" => AiProvider.OpenAI,
                "google" => AiProvider.Google,
                _ => AiProvider.Anthropic
            };

            var commStyleStr = agentData.TryGetProperty("communicationStyle", out var commProp) ? commProp.GetString() : "Formal";
            var communicationStyle = commStyleStr?.ToLower() switch
            {
                "casual" => CommunicationStyle.Casual,
                "direct" => CommunicationStyle.Direct,
                "diplomatic" => CommunicationStyle.Diplomatic,
                "analytical" => CommunicationStyle.Analytical,
                _ => CommunicationStyle.Formal
            };

            var reactionStr = agentData.TryGetProperty("reactionTendency", out var reactProp) ? reactProp.GetString() : "Balanced";
            var reactionTendency = reactionStr?.ToLower() switch
            {
                "supportive" => ReactionTendency.Supportive,
                "critical" => ReactionTendency.Critical,
                "devilsadvocate" => ReactionTendency.DevilsAdvocate,
                "consensusbuilder" => ReactionTendency.ConsensusBuilder,
                _ => ReactionTendency.Balanced
            };

            var agent = new AiAgent
            {
                Name = name,
                RoleTitle = roleTitle,
                SystemPrompt = systemPrompt,
                Provider = provider,
                ModelId = agentData.TryGetProperty("modelId", out var modelProp) ? modelProp.GetString() ?? "claude-sonnet-4-20250514" : "claude-sonnet-4-20250514",
                ModelDisplayName = agentData.TryGetProperty("modelDisplayName", out var dispProp) ? dispProp.GetString() ?? "Claude Sonnet" : "Claude Sonnet",
                AvatarColor = agentData.TryGetProperty("avatarColor", out var colorProp) ? colorProp.GetString() : "#4F46E5",
                MaxTokensPerResponse = agentData.TryGetProperty("maxTokensPerResponse", out var tokensProp) ? tokensProp.GetInt32() : 4096,
                Temperature = agentData.TryGetProperty("temperature", out var tempProp) ? tempProp.GetDecimal() : 0.7m,
                Assertiveness = agentData.TryGetProperty("assertiveness", out var assertProp) ? assertProp.GetInt32() : 50,
                CommunicationStyle = communicationStyle,
                ReactionTendency = reactionTendency,
                ExpertiseAreas = agentData.TryGetProperty("expertiseAreas", out var expertProp) ? expertProp.GetString() : null,
                SeniorityLevel = agentData.TryGetProperty("seniorityLevel", out var seniorProp) ? seniorProp.GetInt32() : 3,
                AsksQuestions = agentData.TryGetProperty("asksQuestions", out var asksProp) && asksProp.GetBoolean(),
                GivesBriefAcknowledgments = !agentData.TryGetProperty("givesBriefAcknowledgments", out var ackProp) || ackProp.GetBoolean(),
                IsActive = !agentData.TryGetProperty("isActive", out var activeProp) || activeProp.GetBoolean(),
                OrganizationId = organizationId
            };

            _dbContext.AiAgents.Add(agent);
            created.Add(name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var message = $"Successfully created {created.Count} AI agents: {string.Join(", ", created)}";
        if (errors.Any())
            message += $"\nSkipped: {string.Join(", ", errors)}";

        return new ToolResult { Action = "bulk_created", Message = message };
    }

    #endregion

    #region Conversation Management Tools

    private async Task<ToolResult> CreateConversationAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var title = input.Value.GetProperty("title").GetString()!;

        // Parse mode
        var modeStr = input.Value.TryGetProperty("mode", out var modeProp) ? modeProp.GetString() : "OnDemand";
        var mode = modeStr?.ToLower() switch
        {
            "moderated" => ConversationMode.Moderated,
            "roundrobin" => ConversationMode.RoundRobin,
            "free" => ConversationMode.Free,
            "emergent" => ConversationMode.Emergent,
            _ => ConversationMode.OnDemand
        };

        // Get a default user for created by (we need to handle this better in the future)
        var defaultUser = await _dbContext.Users.FirstOrDefaultAsync(cancellationToken);
        if (defaultUser == null)
            return new ToolResult { Action = "error", Message = "No users found in the system." };

        var conversation = new Conversation
        {
            Title = title,
            Mode = mode,
            Status = ConversationStatus.Active,
            StartedAt = DateTime.UtcNow,
            MaxTurns = input.Value.TryGetProperty("maxTurns", out var turnsProp) ? turnsProp.GetInt32() : null,
            MaxTokens = input.Value.TryGetProperty("maxTokens", out var tokensProp) ? tokensProp.GetInt64() : null,
            OrganizationId = organizationId,
            CreatedByUserId = defaultUser.Id
        };

        _dbContext.Conversations.Add(conversation);

        // Add user as owner participant
        _dbContext.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationId = conversation.Id,
            ParticipantType = ParticipantType.User,
            UserId = defaultUser.Id,
            Role = ParticipantRole.Owner,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });

        // Add AI agents as participants if provided
        var addedAgents = new List<string>();
        if (input.Value.TryGetProperty("agentIds", out var agentIdsProp))
        {
            foreach (var agentIdElem in agentIdsProp.EnumerateArray())
            {
                var agentId = Guid.Parse(agentIdElem.GetString()!);
                var agent = await _dbContext.AiAgents
                    .FirstOrDefaultAsync(a => a.Id == agentId && a.OrganizationId == organizationId && a.DeletedAt == null, cancellationToken);

                if (agent != null)
                {
                    _dbContext.ConversationParticipants.Add(new ConversationParticipant
                    {
                        ConversationId = conversation.Id,
                        ParticipantType = ParticipantType.Ai,
                        AiAgentId = agentId,
                        Role = ParticipantRole.Participant,
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                    addedAgents.Add(agent.Name);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var message = $"Successfully created conversation **{title}** with {mode} mode.";
        if (addedAgents.Any())
            message += $"\nAdded AI agents: {string.Join(", ", addedAgents)}";

        return new ToolResult
        {
            Action = "created",
            Message = message,
            Data = JsonSerializer.SerializeToElement(new { id = conversation.Id, title = conversation.Title, mode = mode.ToString() })
        };
    }

    private async Task<ToolResult> UpdateConversationAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var conversationId = Guid.Parse(input.Value.GetProperty("conversationId").GetString()!);

        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.OrganizationId == organizationId && c.DeletedAt == null, cancellationToken);

        if (conversation == null)
            return new ToolResult { Action = "error", Message = "Conversation not found." };

        if (input.Value.TryGetProperty("title", out var titleProp))
            conversation.Title = titleProp.GetString()!;

        if (input.Value.TryGetProperty("mode", out var modeProp))
        {
            conversation.Mode = modeProp.GetString()?.ToLower() switch
            {
                "moderated" => ConversationMode.Moderated,
                "roundrobin" => ConversationMode.RoundRobin,
                "free" => ConversationMode.Free,
                "emergent" => ConversationMode.Emergent,
                _ => ConversationMode.OnDemand
            };
        }

        if (input.Value.TryGetProperty("status", out var statusProp))
        {
            conversation.Status = statusProp.GetString()?.ToLower() switch
            {
                "paused" => ConversationStatus.Paused,
                "archived" => ConversationStatus.Archived,
                _ => ConversationStatus.Active
            };
        }

        if (input.Value.TryGetProperty("maxTurns", out var turnsProp))
            conversation.MaxTurns = turnsProp.GetInt32();

        if (input.Value.TryGetProperty("maxTokens", out var tokensProp))
            conversation.MaxTokens = tokensProp.GetInt64();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "updated",
            Message = $"Successfully updated conversation **{conversation.Title}**.",
            Data = JsonSerializer.SerializeToElement(new { id = conversation.Id, title = conversation.Title })
        };
    }

    private async Task<ToolResult> AddAgentsToConversationAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var conversationId = Guid.Parse(input.Value.GetProperty("conversationId").GetString()!);
        var agentIdsProp = input.Value.GetProperty("agentIds");

        var conversation = await _dbContext.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.OrganizationId == organizationId && c.DeletedAt == null, cancellationToken);

        if (conversation == null)
            return new ToolResult { Action = "error", Message = "Conversation not found." };

        var addedAgents = new List<string>();
        var skippedAgents = new List<string>();

        foreach (var agentIdElem in agentIdsProp.EnumerateArray())
        {
            var agentId = Guid.Parse(agentIdElem.GetString()!);

            // Check if agent exists
            var agent = await _dbContext.AiAgents
                .FirstOrDefaultAsync(a => a.Id == agentId && a.OrganizationId == organizationId && a.DeletedAt == null, cancellationToken);

            if (agent == null)
                continue;

            // Check if already a participant
            var existingParticipant = conversation.Participants
                .FirstOrDefault(p => p.AiAgentId == agentId && p.IsActive);

            if (existingParticipant != null)
            {
                skippedAgents.Add($"{agent.Name} (already in conversation)");
                continue;
            }

            _dbContext.ConversationParticipants.Add(new ConversationParticipant
            {
                ConversationId = conversationId,
                ParticipantType = ParticipantType.Ai,
                AiAgentId = agentId,
                Role = ParticipantRole.Participant,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            });
            addedAgents.Add(agent.Name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var message = addedAgents.Any()
            ? $"Added {addedAgents.Count} agents to **{conversation.Title}**: {string.Join(", ", addedAgents)}"
            : "No new agents were added.";

        if (skippedAgents.Any())
            message += $"\nSkipped: {string.Join(", ", skippedAgents)}";

        return new ToolResult { Action = "updated", Message = message };
    }

    private async Task<ToolResult> RemoveAgentsFromConversationAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var conversationId = Guid.Parse(input.Value.GetProperty("conversationId").GetString()!);
        var agentIdsProp = input.Value.GetProperty("agentIds");

        var conversation = await _dbContext.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.OrganizationId == organizationId && c.DeletedAt == null, cancellationToken);

        if (conversation == null)
            return new ToolResult { Action = "error", Message = "Conversation not found." };

        var removedAgents = new List<string>();

        foreach (var agentIdElem in agentIdsProp.EnumerateArray())
        {
            var agentId = Guid.Parse(agentIdElem.GetString()!);

            var participant = conversation.Participants
                .FirstOrDefault(p => p.AiAgentId == agentId && p.IsActive);

            if (participant != null)
            {
                participant.IsActive = false;
                participant.LeftAt = DateTime.UtcNow;

                var agent = await _dbContext.AiAgents.FindAsync(agentId);
                if (agent != null)
                    removedAgents.Add(agent.Name);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "updated",
            Message = removedAgents.Any()
                ? $"Removed {removedAgents.Count} agents from **{conversation.Title}**: {string.Join(", ", removedAgents)}"
                : "No agents were removed."
        };
    }

    private async Task<ToolResult> ListConversationsAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        var query = _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.AiAgent)
            .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null);

        if (input?.TryGetProperty("status", out var statusProp) == true)
        {
            var status = statusProp.GetString()?.ToLower() switch
            {
                "paused" => ConversationStatus.Paused,
                "archived" => ConversationStatus.Archived,
                "active" => ConversationStatus.Active,
                _ => (ConversationStatus?)null
            };

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);
        }

        var conversations = await query
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Title,
                Mode = c.Mode.ToString(),
                Status = c.Status.ToString(),
                c.MessageCount,
                c.LastMessageAt,
                Participants = c.Participants
                    .Where(p => p.IsActive && p.ParticipantType == ParticipantType.Ai)
                    .Select(p => p.AiAgent!.Name)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var convList = string.Join("\n", conversations.Select(c =>
            $"- **{c.Title}** ({c.Mode}, {c.Status}) - {c.MessageCount} messages, {c.Participants.Count} agents: {string.Join(", ", c.Participants)}"));

        return new ToolResult
        {
            Action = "listed",
            Message = conversations.Count == 0
                ? "No conversations found."
                : $"Found {conversations.Count} conversations:\n{convList}",
            Data = JsonSerializer.SerializeToElement(conversations)
        };
    }

    private async Task<ToolResult> DeleteConversationAsync(Guid organizationId, JsonElement? input, CancellationToken cancellationToken)
    {
        if (input == null) return new ToolResult { Action = "error", Message = "No input provided" };

        var conversationId = Guid.Parse(input.Value.GetProperty("conversationId").GetString()!);

        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.OrganizationId == organizationId && c.DeletedAt == null, cancellationToken);

        if (conversation == null)
            return new ToolResult { Action = "error", Message = "Conversation not found." };

        var title = conversation.Title;
        conversation.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "deleted",
            Message = $"Successfully deleted conversation **{title}**."
        };
    }

    #endregion

    #endregion

    // Internal DTOs for Claude API
    private class ClaudeRequest
    {
        public string Model { get; set; } = "";
        public int MaxTokens { get; set; }
        public string System { get; set; } = "";
        public List<ClaudeMessage> Messages { get; set; } = new();
        public List<ClaudeTool>? Tools { get; set; }
    }

    private class ClaudeMessage
    {
        public string Role { get; set; } = "";
        public object Content { get; set; } = "";
    }

    private class ClaudeResponse
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Role { get; set; } = "";
        public List<ClaudeContent> Content { get; set; } = new();
        public string StopReason { get; set; } = "";
    }

    private class ClaudeContent
    {
        public string Type { get; set; } = "";
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Text { get; set; }
        public JsonElement? Input { get; set; }
    }

    private class ClaudeTool
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public ClaudeToolSchema InputSchema { get; set; } = new();
    }

    private class ClaudeToolSchema
    {
        public string Type { get; set; } = "object";
        public Dictionary<string, ClaudeToolProperty> Properties { get; set; } = new();
        public string[] Required { get; set; } = Array.Empty<string>();
    }

    private class ClaudeToolProperty
    {
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
    }

    private class ToolResult
    {
        public required string Action { get; set; }
        public string? Message { get; set; }
        public JsonElement? Data { get; set; }
    }
}
