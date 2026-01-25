using System.Text;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Services;

/// <summary>
/// Service for building organization context that can be shared across AI services
/// </summary>
public interface IOrganizationContextService
{
    /// <summary>
    /// Build the full organization context including all entities
    /// </summary>
    Task<OrganizationContext> BuildContextAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Build a system prompt containing organization context for AI agents
    /// </summary>
    string BuildSystemPrompt(OrganizationContext context, string? agentSystemPrompt = null);
}

public class OrganizationContextService : IOrganizationContextService
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<OrganizationContextService> _logger;

    public OrganizationContextService(
        OrbitOSDbContext dbContext,
        IKnowledgeBaseService knowledgeBaseService,
        ILogger<OrganizationContextService> logger)
    {
        _dbContext = dbContext;
        _knowledgeBaseService = knowledgeBaseService;
        _logger = logger;
    }

    public async Task<OrganizationContext> BuildContextAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var context = new OrganizationContext();

        var org = await _dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        if (org != null)
        {
            context.OrganizationName = org.Name;
        }

        // Only fetch COUNTS for the summary - no detailed data
        // This keeps the system prompt small and forces the AI to use query tools

        // Count People
        var peopleCount = await _dbContext.Resources
            .Where(r => r.OrganizationId == organizationId && r.ResourceSubtype.ResourceType == ResourceType.Person)
            .CountAsync(cancellationToken);
        // Create dummy entries just for count (empty list with correct count)
        context.People = Enumerable.Range(0, peopleCount).Select(_ => new PersonContext()).ToList();

        // Count Roles
        var rolesCount = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .CountAsync(cancellationToken);
        context.Roles = Enumerable.Range(0, rolesCount).Select(_ => new RoleSummary()).ToList();

        // Count Functions
        var functionsCount = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId)
            .CountAsync(cancellationToken);
        context.Functions = Enumerable.Range(0, functionsCount).Select(_ => new FunctionSummary()).ToList();

        // Fetch Person Subtypes (these are needed for creating people, so fetch fully)
        var subtypes = await _dbContext.ResourceSubtypes
            .Where(s => s.OrganizationId == organizationId && s.ResourceType == ResourceType.Person)
            .Select(s => new SubtypeContext { Id = s.Id, Name = s.Name })
            .ToListAsync(cancellationToken);
        context.PersonSubtypes = subtypes;

        // Count Canvases
        var canvasesCount = await _dbContext.Canvases
            .Where(c => c.OrganizationId == organizationId)
            .CountAsync(cancellationToken);
        context.Canvases = Enumerable.Range(0, canvasesCount).Select(_ => new CanvasSummary()).ToList();

        // Count Processes
        var processesCount = await _dbContext.Processes
            .Where(p => p.OrganizationId == organizationId)
            .CountAsync(cancellationToken);
        context.Processes = Enumerable.Range(0, processesCount).Select(_ => new ProcessSummary()).ToList();

        // Count Goals
        var goalsCount = await _dbContext.Goals
            .Where(g => g.OrganizationId == organizationId)
            .CountAsync(cancellationToken);
        context.Goals = Enumerable.Range(0, goalsCount).Select(_ => new GoalSummary()).ToList();

        // Count Partners
        var partnersCount = await _dbContext.Partners
            .Where(p => p.OrganizationId == organizationId)
            .CountAsync(cancellationToken);
        context.Partners = Enumerable.Range(0, partnersCount).Select(_ => new PartnerSummary()).ToList();

        // Count Channels
        var channelsCount = await _dbContext.Channels
            .Where(c => c.OrganizationId == organizationId)
            .CountAsync(cancellationToken);
        context.Channels = Enumerable.Range(0, channelsCount).Select(_ => new ChannelSummary()).ToList();

        // Count Value Propositions
        var valuePropsCount = await _dbContext.ValuePropositions
            .Where(v => v.OrganizationId == organizationId)
            .CountAsync(cancellationToken);
        context.ValuePropositions = Enumerable.Range(0, valuePropsCount).Select(_ => new ValuePropositionSummary()).ToList();

        // Count Customer Relationships
        var customerRelsCount = await _dbContext.CustomerRelationships
            .Where(cr => cr.OrganizationId == organizationId)
            .CountAsync(cancellationToken);
        context.CustomerRelationships = Enumerable.Range(0, customerRelsCount).Select(_ => new CustomerRelationshipSummary()).ToList();

        // Count Revenue Streams
        var revenueStreamsCount = await _dbContext.RevenueStreams
            .Where(rs => rs.OrganizationId == organizationId)
            .CountAsync(cancellationToken);
        context.RevenueStreams = Enumerable.Range(0, revenueStreamsCount).Select(_ => new RevenueStreamSummary()).ToList();

        _logger.LogDebug("Built minimal organization context for {OrgId}: {PeopleCount} people, {FunctionCount} functions, {ProcessCount} processes (counts only)",
            organizationId, peopleCount, functionsCount, processesCount);

        return context;
    }

    public string BuildSystemPrompt(OrganizationContext context, string? agentSystemPrompt = null)
    {
        var sb = new StringBuilder();

        // Start with agent-specific prompt if provided
        if (!string.IsNullOrEmpty(agentSystemPrompt))
        {
            sb.AppendLine("## Your Role and Personality");
            sb.AppendLine(agentSystemPrompt);
            sb.AppendLine();
        }

        sb.AppendLine("## Organization Context");
        sb.AppendLine($"You are helping the organization: **{context.OrganizationName ?? "Unknown Organization"}**");
        sb.AppendLine();

        // Summary counts only - no detailed data to keep token usage low
        sb.AppendLine("## Organization Data Summary");
        sb.AppendLine("This organization has the following data (use data query tools to access details):");
        sb.AppendLine($"- People: {context.People.Count}");
        sb.AppendLine($"- Roles: {context.Roles.Count}");
        sb.AppendLine($"- Functions: {context.Functions.Count}");
        sb.AppendLine($"- Processes: {context.Processes.Count}");
        sb.AppendLine($"- Goals: {context.Goals.Count}");
        sb.AppendLine($"- Partners: {context.Partners.Count}");
        sb.AppendLine($"- Channels: {context.Channels.Count}");
        sb.AppendLine($"- Value Propositions: {context.ValuePropositions.Count}");
        sb.AppendLine($"- Customer Relationships: {context.CustomerRelationships.Count}");
        sb.AppendLine($"- Revenue Streams: {context.RevenueStreams.Count}");
        sb.AppendLine($"- Canvases: {context.Canvases.Count}");
        sb.AppendLine();

        // Person subtypes are needed for creating people
        if (context.PersonSubtypes.Any())
        {
            sb.AppendLine("### Available Person Types (for creating people)");
            foreach (var subtype in context.PersonSubtypes)
                sb.AppendLine($"- {subtype.Name} (ID: {subtype.Id})");
            sb.AppendLine();
        }

        sb.AppendLine("## Data Access - IMPORTANT");
        sb.AppendLine("You do NOT have organization data pre-loaded. Use these tools to query data on-demand:");
        sb.AppendLine("- `get_people` - Get list of people with their roles and capabilities");
        sb.AppendLine("- `get_roles` - Get list of roles with assignments");
        sb.AppendLine("- `get_functions` - Get list of business functions");
        sb.AppendLine("- `get_processes` - Get list of business processes");
        sb.AppendLine("- `get_goals` - Get list of goals/OKRs");
        sb.AppendLine("- `get_partners` - Get list of partners");
        sb.AppendLine("- `get_channels` - Get list of channels");
        sb.AppendLine("- `get_value_propositions` - Get list of value propositions");
        sb.AppendLine("- `get_customer_relationships` - Get list of customer relationships");
        sb.AppendLine("- `get_revenue_streams` - Get list of revenue streams");
        sb.AppendLine("- `get_canvases` - Get list of business model canvases");
        sb.AppendLine("- `get_full_context` - Get ALL organization data (use sparingly, only for comprehensive overviews)");
        sb.AppendLine();
        sb.AppendLine("**Best Practices:**");
        sb.AppendLine("- Query only the data you need to answer the user's question");
        sb.AppendLine("- For specific questions like 'who is the CEO?', use `get_people` with a search filter");
        sb.AppendLine("- For comprehensive analysis, you may need multiple queries");
        sb.AppendLine("- Only use `get_full_context` when explicitly asked for a complete overview");
        sb.AppendLine();

        sb.AppendLine("## Capabilities - What You Can Do");
        sb.AppendLine();
        sb.AppendLine("### People & Organization");
        sb.AppendLine("- Query and understand the organization's people, roles, and functions");
        sb.AppendLine("- Add new people, update their information, assign roles and capabilities");
        sb.AppendLine();
        sb.AppendLine("### Business Functions");
        sb.AppendLine("- Create, update, and delete business functions (capabilities/skills)");
        sb.AppendLine("- Bulk create multiple functions at once");
        sb.AppendLine("- Suggest functions based on industry best practices");
        sb.AppendLine();
        sb.AppendLine("### Business Model Canvas");
        sb.AppendLine("- Create and manage Business Model Canvases");
        sb.AppendLine("- Create Partners, Channels, Value Propositions, Customer Relationships, Revenue Streams");
        sb.AppendLine("- Suggest business model elements based on industry/context");
        sb.AppendLine();
        sb.AppendLine("### Processes & Workflows");
        sb.AppendLine("- Create and manage business processes with activities");
        sb.AppendLine();
        sb.AppendLine("### Goals & OKRs");
        sb.AppendLine("- Create objectives, key results, and initiatives");
        sb.AppendLine();
        sb.AppendLine("### Analysis & Suggestions");
        sb.AppendLine("- Analyze organizational health (coverage gaps, single points of failure)");
        sb.AppendLine("- Generate industry-standard content");
        sb.AppendLine();

        // Knowledge Base Section
        sb.AppendLine("## Knowledge Base");
        sb.AppendLine("You have access to a knowledge base of best practices and guidelines. Use the `lookup_knowledge_base` tool to retrieve detailed content when users ask about methodologies, frameworks, or best practices.");
        sb.AppendLine();
        sb.AppendLine("**Available Topics:**");

        var index = _knowledgeBaseService.GetIndex();
        foreach (var category in index.Categories)
        {
            var articleTitles = string.Join(", ", category.Articles.Select(a => a.Title));
            sb.AppendLine($"- **{category.Name}**: {articleTitles}");
        }
        sb.AppendLine();

        sb.AppendLine("## Response Guidelines");
        sb.AppendLine("- Be concise, helpful, and query data as needed to answer questions");
        sb.AppendLine("- When suggesting changes, explain the reasoning");
        sb.AppendLine("- Format responses with bullet points and clear structure");
        sb.AppendLine("- Always query data before referencing it - don't assume or guess");

        return sb.ToString();
    }
}

#region Context Data Classes

/// <summary>
/// Contains all organization data for AI context
/// </summary>
public class OrganizationContext
{
    public string? OrganizationName { get; set; }
    public List<PersonContext> People { get; set; } = new();
    public List<RoleSummary> Roles { get; set; } = new();
    public List<FunctionSummary> Functions { get; set; } = new();
    public List<SubtypeContext> PersonSubtypes { get; set; } = new();
    public List<CanvasSummary> Canvases { get; set; } = new();
    public List<ProcessSummary> Processes { get; set; } = new();
    public List<GoalSummary> Goals { get; set; } = new();
    public List<PartnerSummary> Partners { get; set; } = new();
    public List<ChannelSummary> Channels { get; set; } = new();
    public List<ValuePropositionSummary> ValuePropositions { get; set; } = new();
    public List<CustomerRelationshipSummary> CustomerRelationships { get; set; } = new();
    public List<RevenueStreamSummary> RevenueStreams { get; set; } = new();
}

public class PersonContext
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public List<RoleContext> Roles { get; set; } = new();
    public List<CapabilityContext> Capabilities { get; set; } = new();
}

public class RoleContext
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal? AllocationPercentage { get; set; }
    public bool IsPrimary { get; set; }
}

public class CapabilityContext
{
    public Guid FunctionId { get; set; }
    public string FunctionName { get; set; } = "";
    public string Level { get; set; } = "";
}

public class RoleSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Department { get; set; }
    public int AssignmentCount { get; set; }
}

public class FunctionSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int CapabilityCount { get; set; }
}

public class SubtypeContext
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class CanvasSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string CanvasType { get; set; } = "";
    public string Status { get; set; } = "";
    public int BlockCount { get; set; }
}

public class ProcessSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public string Status { get; set; } = "";
    public string? OwnerName { get; set; }
    public int ActivityCount { get; set; }
}

public class GoalSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string GoalType { get; set; } = "";
    public string Status { get; set; } = "";
    public string? OwnerName { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? CurrentValue { get; set; }
    public string? Unit { get; set; }
}

public class PartnerSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string StrategicValue { get; set; } = "";
}

public class ChannelSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
    public string Ownership { get; set; } = "";
    public string? PartnerName { get; set; }
}

public class ValuePropositionSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Headline { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "";
}

public class CustomerRelationshipSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
}

public class RevenueStreamSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string PricingMechanism { get; set; } = "";
}

#endregion
