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
    private readonly ILogger<OrganizationContextService> _logger;

    public OrganizationContextService(
        OrbitOSDbContext dbContext,
        ILogger<OrganizationContextService> logger)
    {
        _dbContext = dbContext;
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

        // Fetch People with their roles and capabilities
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

        // Fetch Roles
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

        // Fetch Functions
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

        // Fetch Person Subtypes
        var subtypes = await _dbContext.ResourceSubtypes
            .Where(s => s.OrganizationId == organizationId && s.ResourceType == ResourceType.Person)
            .Select(s => new SubtypeContext { Id = s.Id, Name = s.Name })
            .ToListAsync(cancellationToken);
        context.PersonSubtypes = subtypes;

        // Fetch Canvases
        var canvases = await _dbContext.Canvases
            .Where(c => c.OrganizationId == organizationId)
            .Select(c => new CanvasSummary
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CanvasType = c.CanvasType.ToString(),
                Status = c.Status.ToString(),
                BlockCount = c.Blocks.Count
            })
            .ToListAsync(cancellationToken);
        context.Canvases = canvases;

        // Fetch Processes
        var processes = await _dbContext.Processes
            .Include(p => p.Owner)
            .Where(p => p.OrganizationId == organizationId)
            .Select(p => new ProcessSummary
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Purpose = p.Purpose,
                Status = p.Status.ToString(),
                OwnerName = p.Owner != null ? p.Owner.Name : null,
                ActivityCount = p.Activities.Count
            })
            .ToListAsync(cancellationToken);
        context.Processes = processes;

        // Fetch Goals
        var goals = await _dbContext.Goals
            .Include(g => g.Owner)
            .Where(g => g.OrganizationId == organizationId)
            .Select(g => new GoalSummary
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                GoalType = g.GoalType.ToString(),
                Status = g.Status.ToString(),
                OwnerName = g.Owner != null ? g.Owner.Name : null,
                TargetValue = g.TargetValue,
                CurrentValue = g.CurrentValue,
                Unit = g.Unit
            })
            .ToListAsync(cancellationToken);
        context.Goals = goals;

        // Fetch Partners
        var partners = await _dbContext.Partners
            .Where(p => p.OrganizationId == organizationId)
            .Select(p => new PartnerSummary
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Type = p.Type.ToString(),
                Status = p.Status.ToString(),
                StrategicValue = p.StrategicValue.ToString()
            })
            .ToListAsync(cancellationToken);
        context.Partners = partners;

        // Fetch Channels
        var channels = await _dbContext.Channels
            .Include(c => c.Partner)
            .Where(c => c.OrganizationId == organizationId)
            .Select(c => new ChannelSummary
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Type = c.Type.ToString(),
                Category = c.Category.ToString(),
                Status = c.Status.ToString(),
                Ownership = c.Ownership.ToString(),
                PartnerName = c.Partner != null ? c.Partner.Name : null
            })
            .ToListAsync(cancellationToken);
        context.Channels = channels;

        // Fetch Value Propositions
        var valuePropositions = await _dbContext.ValuePropositions
            .Where(v => v.OrganizationId == organizationId)
            .Select(v => new ValuePropositionSummary
            {
                Id = v.Id,
                Name = v.Name,
                Headline = v.Headline,
                Description = v.Description,
                Status = v.Status.ToString()
            })
            .ToListAsync(cancellationToken);
        context.ValuePropositions = valuePropositions;

        // Fetch Customer Relationships
        var customerRelationships = await _dbContext.CustomerRelationships
            .Where(cr => cr.OrganizationId == organizationId)
            .Select(cr => new CustomerRelationshipSummary
            {
                Id = cr.Id,
                Name = cr.Name,
                Description = cr.Description,
                Type = cr.Type.ToString(),
                Status = cr.Status.ToString()
            })
            .ToListAsync(cancellationToken);
        context.CustomerRelationships = customerRelationships;

        // Fetch Revenue Streams
        var revenueStreams = await _dbContext.RevenueStreams
            .Where(rs => rs.OrganizationId == organizationId)
            .Select(rs => new RevenueStreamSummary
            {
                Id = rs.Id,
                Name = rs.Name,
                Description = rs.Description,
                Type = rs.Type.ToString(),
                Status = rs.Status.ToString(),
                PricingMechanism = rs.PricingMechanism.ToString()
            })
            .ToListAsync(cancellationToken);
        context.RevenueStreams = revenueStreams;

        _logger.LogDebug("Built organization context for {OrgId}: {PeopleCount} people, {FunctionCount} functions, {ProcessCount} processes",
            organizationId, context.People.Count, context.Functions.Count, context.Processes.Count);

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

        sb.AppendLine("## Your Capabilities");
        sb.AppendLine("You have FULL access to all organization data and can help with:");
        sb.AppendLine();
        sb.AppendLine("### People & Organization");
        sb.AppendLine("- Viewing and understanding the organization's people, roles, and functions");
        sb.AppendLine("- Adding new people, updating their information, assigning roles and capabilities");
        sb.AppendLine();
        sb.AppendLine("### Business Functions");
        sb.AppendLine("- Creating, updating, and deleting business functions (capabilities/skills)");
        sb.AppendLine("- Bulk creating multiple functions at once");
        sb.AppendLine("- Suggesting functions based on industry best practices");
        sb.AppendLine();
        sb.AppendLine("### Business Model Canvas");
        sb.AppendLine("- Creating and managing Business Model Canvases");
        sb.AppendLine("- Creating Partners, Channels, Value Propositions, Customer Relationships, Revenue Streams");
        sb.AppendLine("- Suggesting business model elements based on industry/context");
        sb.AppendLine();
        sb.AppendLine("### Processes & Workflows");
        sb.AppendLine("- Creating and managing business processes with activities");
        sb.AppendLine();
        sb.AppendLine("### Goals & OKRs");
        sb.AppendLine("- Creating objectives, key results, and initiatives");
        sb.AppendLine();
        sb.AppendLine("### Analysis & Suggestions");
        sb.AppendLine("- Analyzing organizational health (coverage gaps, single points of failure)");
        sb.AppendLine("- Generating industry-standard content");
        sb.AppendLine();

        sb.AppendLine("## Current Organization Data");
        sb.AppendLine();

        // People section
        sb.AppendLine($"### People ({context.People.Count} total)");
        if (context.People.Any())
        {
            foreach (var person in context.People.Take(20))
            {
                sb.AppendLine($"- **{person.Name}** (ID: {person.Id}, Status: {person.Status})");
                if (person.Roles.Any())
                    sb.AppendLine($"  - Roles: {string.Join(", ", person.Roles.Select(r => r.Name + (r.IsPrimary ? " (Primary)" : "")))}");
                if (person.Capabilities.Any())
                    sb.AppendLine($"  - Capabilities: {string.Join(", ", person.Capabilities.Select(c => c.FunctionName))}");
            }
            if (context.People.Count > 20) sb.AppendLine($"  ...and {context.People.Count - 20} more");
        }
        else sb.AppendLine("No people have been added yet.");
        sb.AppendLine();

        // Roles section
        sb.AppendLine($"### Roles ({context.Roles.Count} total)");
        if (context.Roles.Any())
            foreach (var role in context.Roles)
                sb.AppendLine($"- **{role.Name}** (ID: {role.Id}) - {role.AssignmentCount} assigned{(role.Department != null ? $", Dept: {role.Department}" : "")}");
        else sb.AppendLine("No roles have been defined yet.");
        sb.AppendLine();

        // Functions section
        sb.AppendLine($"### Functions ({context.Functions.Count} total)");
        if (context.Functions.Any())
        {
            var grouped = context.Functions.GroupBy(f => f.Category ?? "Uncategorized");
            foreach (var group in grouped)
            {
                sb.AppendLine($"**{group.Key}:** {string.Join(", ", group.Take(10).Select(f => f.Name))}");
                if (group.Count() > 10) sb.AppendLine($"  ...and {group.Count() - 10} more");
            }
        }
        else sb.AppendLine("No functions have been defined yet.");
        sb.AppendLine();

        // Processes section
        sb.AppendLine($"### Processes ({context.Processes.Count} total)");
        if (context.Processes.Any())
            foreach (var p in context.Processes)
                sb.AppendLine($"- **{p.Name}** (ID: {p.Id}, {p.ActivityCount} activities, Owner: {p.OwnerName ?? "Unassigned"})");
        else sb.AppendLine("No processes have been defined yet.");
        sb.AppendLine();

        // Goals section
        sb.AppendLine($"### Goals ({context.Goals.Count} total)");
        if (context.Goals.Any())
            foreach (var g in context.Goals)
                sb.AppendLine($"- **{g.Name}** (ID: {g.Id}, Type: {g.GoalType}, Status: {g.Status})");
        else sb.AppendLine("No goals have been defined yet.");
        sb.AppendLine();

        // Business Model Canvas Data
        sb.AppendLine("### Business Model Canvas Data");
        sb.AppendLine($"- Canvases: {context.Canvases.Count} ({string.Join(", ", context.Canvases.Take(5).Select(c => c.Name))}{(context.Canvases.Count > 5 ? "..." : "")})");
        sb.AppendLine($"- Partners: {context.Partners.Count} ({string.Join(", ", context.Partners.Take(5).Select(p => p.Name))}{(context.Partners.Count > 5 ? "..." : "")})");
        sb.AppendLine($"- Channels: {context.Channels.Count} ({string.Join(", ", context.Channels.Take(5).Select(c => c.Name))}{(context.Channels.Count > 5 ? "..." : "")})");
        sb.AppendLine($"- Value Propositions: {context.ValuePropositions.Count} ({string.Join(", ", context.ValuePropositions.Take(5).Select(v => v.Name))}{(context.ValuePropositions.Count > 5 ? "..." : "")})");
        sb.AppendLine($"- Customer Relationships: {context.CustomerRelationships.Count} ({string.Join(", ", context.CustomerRelationships.Take(5).Select(c => c.Name))}{(context.CustomerRelationships.Count > 5 ? "..." : "")})");
        sb.AppendLine($"- Revenue Streams: {context.RevenueStreams.Count} ({string.Join(", ", context.RevenueStreams.Take(5).Select(r => r.Name))}{(context.RevenueStreams.Count > 5 ? "..." : "")})");
        sb.AppendLine();

        if (context.PersonSubtypes.Any())
        {
            sb.AppendLine("### Available Person Types");
            foreach (var subtype in context.PersonSubtypes)
                sb.AppendLine($"- {subtype.Name} (ID: {subtype.Id})");
            sb.AppendLine();
        }

        sb.AppendLine("## Response Guidelines");
        sb.AppendLine("- Be concise, helpful, and use the organization data to provide relevant answers");
        sb.AppendLine("- When suggesting changes, explain the reasoning");
        sb.AppendLine("- Format responses with bullet points and clear structure");
        sb.AppendLine("- Reference specific data from the organization when relevant");

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
