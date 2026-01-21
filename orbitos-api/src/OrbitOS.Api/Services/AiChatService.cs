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
    private readonly ILogger<AiChatService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ANTHROPIC_API_URL = "https://api.anthropic.com/v1/messages";

    public AiChatService(
        OrbitOSDbContext dbContext,
        IOrganizationContextService contextService,
        ILogger<AiChatService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _contextService = contextService;
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
            MaxTokens = 4096,
            System = systemPrompt,
            Messages = messages,
            Tools = tools.Count > 0 ? tools : null
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ANTHROPIC_API_URL);
        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Claude API error: {StatusCode} - {Response}", httpResponse.StatusCode, responseJson);
            throw new Exception($"Claude API error: {httpResponse.StatusCode}");
        }

        return JsonSerializer.Deserialize<ClaudeResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        }) ?? throw new Exception("Failed to parse Claude response");
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
                _ => new ToolResult { Action = "unknown", Message = $"Unknown tool: {toolName}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {Tool}", toolName);
            return new ToolResult { Action = "error", Message = $"Error: {ex.Message}" };
        }
    }

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
