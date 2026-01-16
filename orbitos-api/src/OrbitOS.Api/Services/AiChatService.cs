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
    private readonly ILogger<AiChatService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ANTHROPIC_API_URL = "https://api.anthropic.com/v1/messages";

    public AiChatService(
        OrbitOSDbContext dbContext,
        ILogger<AiChatService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
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
            var orgContext = await BuildOrganizationContextAsync(organizationId, request.Context, cancellationToken);
            var systemPrompt = BuildSystemPrompt(orgContext);
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

    private async Task<OrganizationContext> BuildOrganizationContextAsync(
        Guid organizationId,
        string? contextType,
        CancellationToken cancellationToken)
    {
        var context = new OrganizationContext();

        var org = await _dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        if (org != null)
        {
            context.OrganizationName = org.Name;
        }

        var people = await _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .Include(r => r.RoleAssignments)
                .ThenInclude(ra => ra.Role)
            .Include(r => r.FunctionCapabilities)
                .ThenInclude(fc => fc.Function)
            .Where(r => r.OrganizationId == organizationId && r.ResourceSubtype.ResourceType == ResourceType.Person)
            .ToListAsync(cancellationToken);

        context.People = people.Select(p => new PersonContext
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Status = p.Status.ToString(),
            Roles = p.RoleAssignments.Select(ra => new RoleContext
            {
                Id = ra.RoleId,
                Name = ra.Role.Name,
                AllocationPercentage = ra.AllocationPercentage,
                IsPrimary = ra.IsPrimary
            }).ToList(),
            Capabilities = p.FunctionCapabilities.Select(fc => new CapabilityContext
            {
                FunctionId = fc.FunctionId,
                FunctionName = fc.Function.Name,
                Level = fc.Level.ToString()
            }).ToList()
        }).ToList();

        var roles = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .Select(r => new RoleSummary
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Department = r.Department,
                AssignmentCount = _dbContext.RoleAssignments.Count(ra => ra.RoleId == r.Id)
            })
            .ToListAsync(cancellationToken);

        context.Roles = roles;

        var functions = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId)
            .Select(f => new FunctionSummary
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                Category = f.Category,
                CapabilityCount = f.FunctionCapabilities.Count
            })
            .ToListAsync(cancellationToken);

        context.Functions = functions;

        var subtypes = await _dbContext.ResourceSubtypes
            .Where(s => s.OrganizationId == organizationId && s.ResourceType == ResourceType.Person)
            .Select(s => new SubtypeContext { Id = s.Id, Name = s.Name })
            .ToListAsync(cancellationToken);

        context.PersonSubtypes = subtypes;

        return context;
    }

    private string BuildSystemPrompt(OrganizationContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are an AI assistant for OrbitOS, a business operating system that helps organizations manage their operations.");
        sb.AppendLine();
        sb.AppendLine($"You are helping the organization: {context.OrganizationName ?? "Unknown Organization"}");
        sb.AppendLine();
        sb.AppendLine("## Your Capabilities");
        sb.AppendLine("You can help with:");
        sb.AppendLine();
        sb.AppendLine("### People Management");
        sb.AppendLine("- Viewing and understanding the organization's people, roles, and functions");
        sb.AppendLine("- Adding new people to the organization");
        sb.AppendLine("- Updating existing people's information");
        sb.AppendLine("- Assigning roles to people");
        sb.AppendLine("- Adding function capabilities to people");
        sb.AppendLine();
        sb.AppendLine("### Function Management");
        sb.AppendLine("- Creating new business functions (capabilities/skills)");
        sb.AppendLine("- Updating existing functions (name, description, category)");
        sb.AppendLine("- Bulk creating multiple functions at once");
        sb.AppendLine("- Suggesting new functions based on organization context");
        sb.AppendLine("- Analyzing and suggesting improvements for existing functions");
        sb.AppendLine("- Deleting functions");
        sb.AppendLine();
        sb.AppendLine("### Analysis");
        sb.AppendLine("- Analyzing organizational health (coverage gaps, single points of failure)");
        sb.AppendLine();
        sb.AppendLine("## Current Organization Data");
        sb.AppendLine();

        sb.AppendLine($"### People ({context.People.Count} total)");
        if (context.People.Any())
        {
            foreach (var person in context.People)
            {
                sb.AppendLine($"- **{person.Name}** (ID: {person.Id}, Status: {person.Status})");
                if (person.Roles.Any())
                    sb.AppendLine($"  - Roles: {string.Join(", ", person.Roles.Select(r => r.Name + (r.IsPrimary ? " (Primary)" : "")))}");
                if (person.Capabilities.Any())
                    sb.AppendLine($"  - Capabilities: {string.Join(", ", person.Capabilities.Select(c => c.FunctionName))}");
            }
        }
        else
        {
            sb.AppendLine("No people have been added yet.");
        }
        sb.AppendLine();

        sb.AppendLine($"### Roles ({context.Roles.Count} total)");
        if (context.Roles.Any())
        {
            foreach (var role in context.Roles)
            {
                sb.AppendLine($"- **{role.Name}** (ID: {role.Id}) - {role.AssignmentCount} people assigned");
                if (!string.IsNullOrEmpty(role.Department))
                    sb.AppendLine($"  - Department: {role.Department}");
            }
        }
        else
        {
            sb.AppendLine("No roles have been defined yet.");
        }
        sb.AppendLine();

        sb.AppendLine($"### Functions ({context.Functions.Count} total)");
        if (context.Functions.Any())
        {
            foreach (var func in context.Functions)
            {
                sb.AppendLine($"- **{func.Name}** (ID: {func.Id}) - {func.CapabilityCount} people capable");
                if (!string.IsNullOrEmpty(func.Category))
                    sb.AppendLine($"  - Category: {func.Category}");
            }
        }
        else
        {
            sb.AppendLine("No functions have been defined yet.");
        }
        sb.AppendLine();

        if (context.PersonSubtypes.Any())
        {
            sb.AppendLine("### Available Person Types");
            foreach (var subtype in context.PersonSubtypes)
                sb.AppendLine($"- {subtype.Name} (ID: {subtype.Id})");
            sb.AppendLine();
        }

        sb.AppendLine("## Response Guidelines");
        sb.AppendLine("- Be concise and helpful");
        sb.AppendLine("- When asked to make changes, use the appropriate tool");
        sb.AppendLine("- Provide actionable insights about organizational health");
        sb.AppendLine("- Format responses nicely with bullet points and bold text");

        return sb.ToString();
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

        _dbContext.Functions.Remove(func);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToolResult
        {
            Action = "deleted",
            Message = $"Successfully deleted function **{name}**."
        };
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

    private class OrganizationContext
    {
        public string? OrganizationName { get; set; }
        public List<PersonContext> People { get; set; } = new();
        public List<RoleSummary> Roles { get; set; } = new();
        public List<FunctionSummary> Functions { get; set; } = new();
        public List<SubtypeContext> PersonSubtypes { get; set; } = new();
    }

    private class PersonContext
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Status { get; set; } = "";
        public List<RoleContext> Roles { get; set; } = new();
        public List<CapabilityContext> Capabilities { get; set; } = new();
    }

    private class RoleContext
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public decimal? AllocationPercentage { get; set; }
        public bool IsPrimary { get; set; }
    }

    private class CapabilityContext
    {
        public Guid FunctionId { get; set; }
        public string FunctionName { get; set; } = "";
        public string Level { get; set; } = "";
    }

    private class RoleSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? Department { get; set; }
        public int AssignmentCount { get; set; }
    }

    private class FunctionSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? Category { get; set; }
        public int CapabilityCount { get; set; }
    }

    private class SubtypeContext
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }
}
