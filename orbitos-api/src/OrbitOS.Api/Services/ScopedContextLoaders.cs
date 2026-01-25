using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Services;

/// <summary>
/// Interface for loading scoped context for Built-in agents
/// </summary>
public interface IScopedContextLoader
{
    /// <summary>
    /// Loads context data for the specified scopes
    /// </summary>
    Task<ScopedContext> LoadContextAsync(Guid organizationId, string[] scopes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a context string for injection into agent prompts
    /// </summary>
    string BuildContextString(ScopedContext context);
}

/// <summary>
/// Context data loaded for specific scopes
/// </summary>
public class ScopedContext
{
    public string OrganizationName { get; set; } = "";
    public List<PersonData> People { get; set; } = new();
    public List<RoleData> Roles { get; set; } = new();
    public List<FunctionData> Functions { get; set; } = new();
    public List<ProcessData> Processes { get; set; } = new();
    public List<ActivityData> Activities { get; set; } = new();
    public List<CanvasData> Canvases { get; set; } = new();
    public List<GoalData> Goals { get; set; } = new();
    public List<PartnerData> Partners { get; set; } = new();
    public List<ChannelData> Channels { get; set; } = new();
    public List<ValuePropositionData> ValuePropositions { get; set; } = new();
    public List<CustomerRelationshipData> CustomerRelationships { get; set; } = new();
    public List<RevenueStreamData> RevenueStreams { get; set; } = new();
    public List<string> LoadedScopes { get; set; } = new();
}

#region Data Classes

public class PersonData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAllocation { get; set; }
    public string AllocationStatus { get; set; } = ""; // Available, Stable, NearCapacity, Overloaded
    public List<RoleAssignmentData> RoleAssignments { get; set; } = new();
    public List<CapabilityData> Capabilities { get; set; } = new();
    public Guid? ReportsToId { get; set; }
    public string? ReportsToName { get; set; }
    public int DirectReportCount { get; set; }
}

public class RoleAssignmentData
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public decimal AllocationPercentage { get; set; }
    public bool IsPrimary { get; set; }
}

public class CapabilityData
{
    public Guid FunctionId { get; set; }
    public string FunctionName { get; set; } = "";
    public string Level { get; set; } = "";
}

public class RoleData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Department { get; set; }
    public int AssigneeCount { get; set; }
    public List<string> AssigneeNames { get; set; } = new();
    public List<string> RequiredFunctions { get; set; } = new();
}

public class FunctionData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int CapablePersonCount { get; set; }
    public List<string> CapablePersonNames { get; set; } = new();
    public bool IsSinglePointOfFailure { get; set; }
}

public class ProcessData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public string Status { get; set; } = "";
    public string StateType { get; set; } = "";
    public Guid? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public int ActivityCount { get; set; }
}

public class ActivityData
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public string ProcessName { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string ActivityType { get; set; } = "";
    public Guid? AssignedRoleId { get; set; }
    public string? AssignedRoleName { get; set; }
    public int SequenceOrder { get; set; }
}

public class CanvasData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string CanvasType { get; set; } = "";
    public string Status { get; set; } = "";
    public int BlockCount { get; set; }
}

public class GoalData
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
    public decimal? ProgressPercentage { get; set; }
}

public class PartnerData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string StrategicValue { get; set; } = "";
}

public class ChannelData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
}

public class ValuePropositionData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Headline { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "";
}

public class CustomerRelationshipData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
}

public class RevenueStreamData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string PricingMechanism { get; set; } = "";
}

#endregion

public class ScopedContextLoader : IScopedContextLoader
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<ScopedContextLoader> _logger;

    public ScopedContextLoader(OrbitOSDbContext dbContext, ILogger<ScopedContextLoader> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ScopedContext> LoadContextAsync(Guid organizationId, string[] scopes, CancellationToken cancellationToken = default)
    {
        var context = new ScopedContext { LoadedScopes = scopes.ToList() };

        // Load organization name
        var org = await _dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);
        context.OrganizationName = org?.Name ?? "Unknown Organization";

        // Load each scope
        foreach (var scope in scopes)
        {
            switch (scope)
            {
                case ContextScopes.Resources:
                    context.People = await LoadPeopleAsync(organizationId, cancellationToken);
                    break;
                case ContextScopes.Roles:
                    context.Roles = await LoadRolesAsync(organizationId, cancellationToken);
                    break;
                case ContextScopes.Functions:
                    context.Functions = await LoadFunctionsAsync(organizationId, cancellationToken);
                    break;
                case ContextScopes.Processes:
                    context.Processes = await LoadProcessesAsync(organizationId, cancellationToken);
                    break;
                case ContextScopes.Activities:
                    context.Activities = await LoadActivitiesAsync(organizationId, cancellationToken);
                    break;
                case ContextScopes.Canvases:
                    context.Canvases = await LoadCanvasesAsync(organizationId, cancellationToken);
                    break;
                case ContextScopes.Goals:
                    context.Goals = await LoadGoalsAsync(organizationId, cancellationToken);
                    break;
                case ContextScopes.Partners:
                    context.Partners = await LoadPartnersAsync(organizationId, cancellationToken);
                    break;
                case ContextScopes.Channels:
                    context.Channels = await LoadChannelsAsync(organizationId, cancellationToken);
                    break;
                case ContextScopes.ValuePropositions:
                    context.ValuePropositions = await LoadValuePropositionsAsync(organizationId, cancellationToken);
                    break;
                case ContextScopes.CustomerRelationships:
                    context.CustomerRelationships = await LoadCustomerRelationshipsAsync(organizationId, cancellationToken);
                    break;
                case ContextScopes.RevenueStreams:
                    context.RevenueStreams = await LoadRevenueStreamsAsync(organizationId, cancellationToken);
                    break;
            }
        }

        _logger.LogDebug("Loaded scoped context for org {OrgId}: {Scopes}", organizationId, string.Join(", ", scopes));
        return context;
    }

    public string BuildContextString(ScopedContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"## Organization: {context.OrganizationName}");
        sb.AppendLine();

        // People
        if (context.People.Any())
        {
            sb.AppendLine($"### People ({context.People.Count})");
            foreach (var person in context.People)
            {
                var roles = person.RoleAssignments.Any()
                    ? string.Join(", ", person.RoleAssignments.Select(r => $"{r.RoleName} ({r.AllocationPercentage}%)"))
                    : "No roles assigned";
                var capabilities = person.Capabilities.Any()
                    ? string.Join(", ", person.Capabilities.Select(c => $"{c.FunctionName} ({c.Level})"))
                    : "";

                sb.AppendLine($"- **{person.Name}** (ID: {person.Id})");
                sb.AppendLine($"  - Status: {person.Status}, Allocation: {person.TotalAllocation}% ({person.AllocationStatus})");
                sb.AppendLine($"  - Roles: {roles}");
                if (!string.IsNullOrEmpty(capabilities))
                    sb.AppendLine($"  - Capabilities: {capabilities}");
                if (person.ReportsToName != null)
                    sb.AppendLine($"  - Reports to: {person.ReportsToName}");
                if (person.DirectReportCount > 0)
                    sb.AppendLine($"  - Direct reports: {person.DirectReportCount}");
            }
            sb.AppendLine();
        }

        // Roles
        if (context.Roles.Any())
        {
            sb.AppendLine($"### Roles ({context.Roles.Count})");
            foreach (var role in context.Roles)
            {
                var assignees = role.AssigneeNames.Any()
                    ? string.Join(", ", role.AssigneeNames)
                    : "No one assigned";
                sb.AppendLine($"- **{role.Name}** (ID: {role.Id}): {role.AssigneeCount} people - {assignees}");
                if (!string.IsNullOrEmpty(role.Department))
                    sb.AppendLine($"  - Department: {role.Department}");
            }
            sb.AppendLine();
        }

        // Functions
        if (context.Functions.Any())
        {
            sb.AppendLine($"### Functions ({context.Functions.Count})");
            foreach (var func in context.Functions)
            {
                var spofWarning = func.IsSinglePointOfFailure ? " ⚠️ SPOF" : "";
                var capable = func.CapablePersonNames.Any()
                    ? string.Join(", ", func.CapablePersonNames)
                    : "No one capable";
                sb.AppendLine($"- **{func.Name}** (ID: {func.Id}): {func.CapablePersonCount} capable{spofWarning} - {capable}");
            }
            sb.AppendLine();
        }

        // Processes
        if (context.Processes.Any())
        {
            sb.AppendLine($"### Processes ({context.Processes.Count})");
            foreach (var process in context.Processes)
            {
                sb.AppendLine($"- **{process.Name}** (ID: {process.Id})");
                sb.AppendLine($"  - Status: {process.Status}, Type: {process.StateType}");
                sb.AppendLine($"  - Activities: {process.ActivityCount}");
                if (process.OwnerName != null)
                    sb.AppendLine($"  - Owner: {process.OwnerName}");
            }
            sb.AppendLine();
        }

        // Activities
        if (context.Activities.Any())
        {
            sb.AppendLine($"### Activities ({context.Activities.Count})");
            var byProcess = context.Activities.GroupBy(a => a.ProcessName);
            foreach (var group in byProcess)
            {
                sb.AppendLine($"**{group.Key}:**");
                foreach (var activity in group.OrderBy(a => a.SequenceOrder))
                {
                    var assignee = activity.AssignedRoleName ?? "Unassigned";
                    sb.AppendLine($"  {activity.SequenceOrder}. {activity.Name} ({activity.ActivityType}) - {assignee}");
                }
            }
            sb.AppendLine();
        }

        // Goals
        if (context.Goals.Any())
        {
            sb.AppendLine($"### Goals ({context.Goals.Count})");
            foreach (var goal in context.Goals)
            {
                var progress = goal.ProgressPercentage.HasValue
                    ? $"{goal.ProgressPercentage:F0}% complete"
                    : "No progress data";
                sb.AppendLine($"- **{goal.Name}** ({goal.GoalType}): {goal.Status} - {progress}");
                if (goal.TargetValue.HasValue && goal.CurrentValue.HasValue)
                    sb.AppendLine($"  - Target: {goal.TargetValue} {goal.Unit}, Current: {goal.CurrentValue} {goal.Unit}");
            }
            sb.AppendLine();
        }

        // Canvases
        if (context.Canvases.Any())
        {
            sb.AppendLine($"### Business Canvases ({context.Canvases.Count})");
            foreach (var canvas in context.Canvases)
            {
                sb.AppendLine($"- **{canvas.Name}** ({canvas.CanvasType}): {canvas.Status}, {canvas.BlockCount} blocks");
            }
            sb.AppendLine();
        }

        // Partners
        if (context.Partners.Any())
        {
            sb.AppendLine($"### Partners ({context.Partners.Count})");
            foreach (var partner in context.Partners)
            {
                sb.AppendLine($"- **{partner.Name}** ({partner.Type}): {partner.Status}, Strategic Value: {partner.StrategicValue}");
            }
            sb.AppendLine();
        }

        // Channels
        if (context.Channels.Any())
        {
            sb.AppendLine($"### Channels ({context.Channels.Count})");
            foreach (var channel in context.Channels)
            {
                sb.AppendLine($"- **{channel.Name}** ({channel.Type}/{channel.Category}): {channel.Status}");
            }
            sb.AppendLine();
        }

        // Value Propositions
        if (context.ValuePropositions.Any())
        {
            sb.AppendLine($"### Value Propositions ({context.ValuePropositions.Count})");
            foreach (var vp in context.ValuePropositions)
            {
                sb.AppendLine($"- **{vp.Name}**: {vp.Headline} ({vp.Status})");
            }
            sb.AppendLine();
        }

        // Customer Relationships
        if (context.CustomerRelationships.Any())
        {
            sb.AppendLine($"### Customer Relationships ({context.CustomerRelationships.Count})");
            foreach (var cr in context.CustomerRelationships)
            {
                sb.AppendLine($"- **{cr.Name}** ({cr.Type}): {cr.Status}");
            }
            sb.AppendLine();
        }

        // Revenue Streams
        if (context.RevenueStreams.Any())
        {
            sb.AppendLine($"### Revenue Streams ({context.RevenueStreams.Count})");
            foreach (var rs in context.RevenueStreams)
            {
                sb.AppendLine($"- **{rs.Name}** ({rs.Type}): {rs.Status}, Pricing: {rs.PricingMechanism}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    #region Private Loader Methods

    private async Task<List<PersonData>> LoadPeopleAsync(Guid orgId, CancellationToken ct)
    {
        var people = await _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .Include(r => r.RoleAssignments)
                .ThenInclude(ra => ra.Role)
            .Include(r => r.FunctionCapabilities)
                .ThenInclude(c => c.Function)
            .Include(r => r.ReportsToResource)
            .Where(r => r.OrganizationId == orgId && r.ResourceSubtype.ResourceType == ResourceType.Person)
            .ToListAsync(ct);

        var result = new List<PersonData>();
        foreach (var person in people)
        {
            var totalAlloc = person.RoleAssignments.Sum(ra => ra.AllocationPercentage ?? 0);
            var status = totalAlloc switch
            {
                >= 100 => "Overloaded",
                >= 80 => "NearCapacity",
                >= 50 => "Stable",
                _ => "Available"
            };

            var directReports = people.Count(p => p.ReportsToResourceId == person.Id);

            result.Add(new PersonData
            {
                Id = person.Id,
                Name = person.Name,
                Description = person.Description,
                Status = person.Status.ToString(),
                TotalAllocation = totalAlloc,
                AllocationStatus = status,
                RoleAssignments = person.RoleAssignments.Select(ra => new RoleAssignmentData
                {
                    RoleId = ra.RoleId,
                    RoleName = ra.Role.Name,
                    AllocationPercentage = ra.AllocationPercentage ?? 0,
                    IsPrimary = ra.IsPrimary
                }).ToList(),
                Capabilities = person.FunctionCapabilities.Select(c => new CapabilityData
                {
                    FunctionId = c.FunctionId,
                    FunctionName = c.Function.Name,
                    Level = c.Level.ToString()
                }).ToList(),
                ReportsToId = person.ReportsToResourceId,
                ReportsToName = person.ReportsToResource?.Name,
                DirectReportCount = directReports
            });
        }

        return result;
    }

    private async Task<List<RoleData>> LoadRolesAsync(Guid orgId, CancellationToken ct)
    {
        return await _dbContext.Roles
            .Include(r => r.RoleAssignments)
                .ThenInclude(ra => ra.Resource)
            .Include(r => r.RoleFunctions)
                .ThenInclude(rf => rf.Function)
            .Where(r => r.OrganizationId == orgId)
            .Select(r => new RoleData
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Department = r.Department,
                AssigneeCount = r.RoleAssignments.Count,
                AssigneeNames = r.RoleAssignments.Select(ra => ra.Resource.Name).ToList(),
                RequiredFunctions = r.RoleFunctions.Select(rf => rf.Function.Name).ToList()
            })
            .ToListAsync(ct);
    }

    private async Task<List<FunctionData>> LoadFunctionsAsync(Guid orgId, CancellationToken ct)
    {
        return await _dbContext.Functions
            .Include(f => f.FunctionCapabilities)
                .ThenInclude(c => c.Resource)
            .Where(f => f.OrganizationId == orgId)
            .Select(f => new FunctionData
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                Category = f.Category,
                CapablePersonCount = f.FunctionCapabilities.Count,
                CapablePersonNames = f.FunctionCapabilities.Select(c => c.Resource.Name).ToList(),
                IsSinglePointOfFailure = f.FunctionCapabilities.Count == 1
            })
            .ToListAsync(ct);
    }

    private async Task<List<ProcessData>> LoadProcessesAsync(Guid orgId, CancellationToken ct)
    {
        return await _dbContext.Processes
            .Include(p => p.Owner)
            .Include(p => p.Activities)
            .Where(p => p.OrganizationId == orgId)
            .Select(p => new ProcessData
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Purpose = p.Purpose,
                Status = p.Status.ToString(),
                StateType = p.StateType.ToString(),
                OwnerId = p.OwnerId,
                OwnerName = p.Owner != null ? p.Owner.Name : null,
                ActivityCount = p.Activities.Count
            })
            .ToListAsync(ct);
    }

    private async Task<List<ActivityData>> LoadActivitiesAsync(Guid orgId, CancellationToken ct)
    {
        return await _dbContext.Activities
            .Include(a => a.Process)
            .Include(a => a.AssignedResource)
            .Where(a => a.Process.OrganizationId == orgId)
            .Select(a => new ActivityData
            {
                Id = a.Id,
                ProcessId = a.ProcessId,
                ProcessName = a.Process.Name,
                Name = a.Name,
                Description = a.Description,
                ActivityType = a.ActivityType.ToString(),
                AssignedRoleId = a.AssignedResourceId,
                AssignedRoleName = a.AssignedResource != null ? a.AssignedResource.Name : null,
                SequenceOrder = a.Order
            })
            .ToListAsync(ct);
    }

    private async Task<List<CanvasData>> LoadCanvasesAsync(Guid orgId, CancellationToken ct)
    {
        return await _dbContext.Canvases
            .Include(c => c.Blocks)
            .Where(c => c.OrganizationId == orgId)
            .Select(c => new CanvasData
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CanvasType = c.CanvasType.ToString(),
                Status = c.Status.ToString(),
                BlockCount = c.Blocks.Count
            })
            .ToListAsync(ct);
    }

    private async Task<List<GoalData>> LoadGoalsAsync(Guid orgId, CancellationToken ct)
    {
        return await _dbContext.Goals
            .Include(g => g.Owner)
            .Where(g => g.OrganizationId == orgId)
            .Select(g => new GoalData
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                GoalType = g.GoalType.ToString(),
                Status = g.Status.ToString(),
                OwnerName = g.Owner != null ? g.Owner.Name : null,
                TargetValue = g.TargetValue,
                CurrentValue = g.CurrentValue,
                Unit = g.Unit,
                ProgressPercentage = g.TargetValue.HasValue && g.TargetValue > 0 && g.CurrentValue.HasValue
                    ? (g.CurrentValue / g.TargetValue) * 100
                    : null
            })
            .ToListAsync(ct);
    }

    private async Task<List<PartnerData>> LoadPartnersAsync(Guid orgId, CancellationToken ct)
    {
        return await _dbContext.Partners
            .Where(p => p.OrganizationId == orgId)
            .Select(p => new PartnerData
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Type = p.Type.ToString(),
                Status = p.Status.ToString(),
                StrategicValue = p.StrategicValue.ToString()
            })
            .ToListAsync(ct);
    }

    private async Task<List<ChannelData>> LoadChannelsAsync(Guid orgId, CancellationToken ct)
    {
        return await _dbContext.Channels
            .Where(c => c.OrganizationId == orgId)
            .Select(c => new ChannelData
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Type = c.Type.ToString(),
                Category = c.Category.ToString(),
                Status = c.Status.ToString()
            })
            .ToListAsync(ct);
    }

    private async Task<List<ValuePropositionData>> LoadValuePropositionsAsync(Guid orgId, CancellationToken ct)
    {
        return await _dbContext.ValuePropositions
            .Where(v => v.OrganizationId == orgId)
            .Select(v => new ValuePropositionData
            {
                Id = v.Id,
                Name = v.Name,
                Headline = v.Headline,
                Description = v.Description,
                Status = v.Status.ToString()
            })
            .ToListAsync(ct);
    }

    private async Task<List<CustomerRelationshipData>> LoadCustomerRelationshipsAsync(Guid orgId, CancellationToken ct)
    {
        return await _dbContext.CustomerRelationships
            .Where(cr => cr.OrganizationId == orgId)
            .Select(cr => new CustomerRelationshipData
            {
                Id = cr.Id,
                Name = cr.Name,
                Description = cr.Description,
                Type = cr.Type.ToString(),
                Status = cr.Status.ToString()
            })
            .ToListAsync(ct);
    }

    private async Task<List<RevenueStreamData>> LoadRevenueStreamsAsync(Guid orgId, CancellationToken ct)
    {
        return await _dbContext.RevenueStreams
            .Where(rs => rs.OrganizationId == orgId)
            .Select(rs => new RevenueStreamData
            {
                Id = rs.Id,
                Name = rs.Name,
                Description = rs.Description,
                Type = rs.Type.ToString(),
                Status = rs.Status.ToString(),
                PricingMechanism = rs.PricingMechanism.ToString()
            })
            .ToListAsync(ct);
    }

    #endregion
}
