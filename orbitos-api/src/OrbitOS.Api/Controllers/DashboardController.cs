using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers;

/// <summary>
/// Provides dashboard data endpoints for progressive loading.
/// Each endpoint returns a subset of dashboard data for independent widget loading.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/dashboard")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Dashboard")]
public class DashboardController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(OrbitOSDbContext dbContext, ILogger<DashboardController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get quick stats for the dashboard (people, roles, functions, processes counts).
    /// This is a lightweight endpoint that returns immediately.
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats(Guid organizationId)
    {
        var peopleCount = await _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .CountAsync(r => r.OrganizationId == organizationId && r.ResourceSubtype.ResourceType == ResourceType.Person);

        var rolesCount = await _dbContext.Roles
            .CountAsync(r => r.OrganizationId == organizationId);

        var functionsCount = await _dbContext.Functions
            .CountAsync(f => f.OrganizationId == organizationId);

        var processesCount = await _dbContext.Processes
            .CountAsync(p => p.OrganizationId == organizationId);

        // Calculate change indicators
        var emptyRolesCount = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .Where(r => !_dbContext.RoleAssignments.Any(ra => ra.RoleId == r.Id))
            .CountAsync();

        var spofRolesCount = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .Where(r => _dbContext.RoleAssignments.Count(ra => ra.RoleId == r.Id) == 1)
            .CountAsync();

        var uncoveredFunctionsCount = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId)
            .Where(f => !f.FunctionCapabilities.Any())
            .CountAsync();

        var processesNeedingReview = await _dbContext.Processes
            .Where(p => p.OrganizationId == organizationId)
            .Where(p => p.OwnerId == null || !p.Activities.Any())
            .CountAsync();

        return Ok(new DashboardStatsDto
        {
            PeopleCount = peopleCount,
            PeopleChange = peopleCount > 0 ? "Active" : "Add people to get started",
            RolesCount = rolesCount,
            RolesChange = emptyRolesCount > 0 ? $"{emptyRolesCount} unassigned" : (spofRolesCount > 0 ? $"{spofRolesCount} SPOF" : "All covered"),
            FunctionsCount = functionsCount,
            FunctionsChange = uncoveredFunctionsCount > 0 ? $"{uncoveredFunctionsCount} uncovered" : "All mapped",
            ProcessesCount = processesCount,
            ProcessesChange = processesNeedingReview > 0 ? $"{processesNeedingReview} need review" : "All documented"
        });
    }

    /// <summary>
    /// Get focus items for the dashboard (high-impact areas requiring attention).
    /// </summary>
    [HttpGet("focus")]
    public async Task<ActionResult<DashboardFocusDto>> GetFocusItems(Guid organizationId)
    {
        var focusItems = new List<FocusItemDto>();

        // Check for SPOF roles (single point of failure)
        var spofRole = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .Where(r => _dbContext.RoleAssignments.Count(ra => ra.RoleId == r.Id) == 1)
            .Select(r => new
            {
                r.Name,
                AssigneeName = _dbContext.RoleAssignments
                    .Where(ra => ra.RoleId == r.Id)
                    .Select(ra => ra.Resource.Name)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (spofRole != null)
        {
            focusItems.Add(new FocusItemDto
            {
                Title = $"{spofRole.Name} Coverage",
                Status = "Bottleneck",
                Summary = $"Only {spofRole.AssigneeName} covers this role. Add backup to reduce risk.",
                Href = "/app/roles"
            });
        }

        // Check for empty roles
        var emptyRolesCount = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .Where(r => !_dbContext.RoleAssignments.Any(ra => ra.RoleId == r.Id))
            .CountAsync();

        if (emptyRolesCount > 0)
        {
            focusItems.Add(new FocusItemDto
            {
                Title = "Role Assignments",
                Status = "Review",
                Summary = $"{emptyRolesCount} role(s) have no one assigned.",
                Href = "/app/roles"
            });
        }

        // Check for uncovered functions
        var uncoveredFunctionsCount = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId)
            .Where(f => !f.FunctionCapabilities.Any())
            .CountAsync();

        if (uncoveredFunctionsCount > 0)
        {
            focusItems.Add(new FocusItemDto
            {
                Title = "Function Coverage",
                Status = "Review",
                Summary = $"{uncoveredFunctionsCount} function(s) have no trained personnel.",
                Href = "/app/functions"
            });
        }

        // Check for processes needing attention
        var processNeedingAttention = await _dbContext.Processes
            .Where(p => p.OrganizationId == organizationId)
            .Where(p => p.OwnerId == null || !p.Activities.Any())
            .Select(p => new { p.Id, p.Name, HasOwner = p.OwnerId != null })
            .FirstOrDefaultAsync();

        if (processNeedingAttention != null)
        {
            focusItems.Add(new FocusItemDto
            {
                Title = processNeedingAttention.Name,
                Status = !processNeedingAttention.HasOwner ? "Bottleneck" : "Review",
                Summary = !processNeedingAttention.HasOwner ? "Process has no owner assigned." : "Process needs activities defined.",
                Href = $"/app/processes/{processNeedingAttention.Id}"
            });
        }

        // If no issues, show healthy state
        if (!focusItems.Any())
        {
            var totalPeople = await _dbContext.Resources
                .Include(r => r.ResourceSubtype)
                .CountAsync(r => r.OrganizationId == organizationId && r.ResourceSubtype.ResourceType == ResourceType.Person);

            var totalRoles = await _dbContext.Roles.CountAsync(r => r.OrganizationId == organizationId);

            focusItems.Add(totalPeople > 0 && totalRoles > 0
                ? new FocusItemDto
                {
                    Title = "Organization Health",
                    Status = "Stable",
                    Summary = "All roles covered. No critical gaps.",
                    Href = "/app/people"
                }
                : new FocusItemDto
                {
                    Title = "Getting Started",
                    Status = "Review",
                    Summary = "Add people and roles to begin.",
                    Href = "/app/people"
                });
        }

        return Ok(new DashboardFocusDto
        {
            FocusItems = focusItems.Take(3).ToList()
        });
    }

    /// <summary>
    /// Get next actions for the dashboard (recommended steps).
    /// </summary>
    [HttpGet("actions")]
    public async Task<ActionResult<DashboardActionsDto>> GetNextActions(Guid organizationId)
    {
        var actions = new List<NextActionDto>();

        // Get empty roles
        var emptyRoles = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .Where(r => !_dbContext.RoleAssignments.Any(ra => ra.RoleId == r.Id))
            .Select(r => r.Name)
            .Take(2)
            .ToListAsync();

        foreach (var roleName in emptyRoles)
        {
            actions.Add(new NextActionDto
            {
                Action = $"Assign someone to the {roleName} role.",
                Href = "/app/roles"
            });
        }

        // Get SPOF roles
        var spofRoles = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .Where(r => _dbContext.RoleAssignments.Count(ra => ra.RoleId == r.Id) == 1)
            .Select(r => new
            {
                r.Name,
                AssigneeName = _dbContext.RoleAssignments
                    .Where(ra => ra.RoleId == r.Id)
                    .Select(ra => ra.Resource.Name)
                    .FirstOrDefault()
            })
            .Take(2)
            .ToListAsync();

        foreach (var role in spofRoles)
        {
            if (actions.Count < 5)
            {
                actions.Add(new NextActionDto
                {
                    Action = $"Add backup for {role.Name} (only {role.AssigneeName}).",
                    Href = "/app/roles"
                });
            }
        }

        // Get uncovered functions
        var uncoveredFunctions = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId)
            .Where(f => !f.FunctionCapabilities.Any())
            .Select(f => f.Name)
            .Take(2)
            .ToListAsync();

        foreach (var funcName in uncoveredFunctions)
        {
            if (actions.Count < 5)
            {
                actions.Add(new NextActionDto
                {
                    Action = $"Train someone on {funcName}.",
                    Href = "/app/functions"
                });
            }
        }

        // Get processes without owners
        var processesWithoutOwner = await _dbContext.Processes
            .Where(p => p.OrganizationId == organizationId)
            .Where(p => p.OwnerId == null)
            .Select(p => new { p.Id, p.Name })
            .Take(2)
            .ToListAsync();

        foreach (var process in processesWithoutOwner)
        {
            if (actions.Count < 5)
            {
                actions.Add(new NextActionDto
                {
                    Action = $"Assign owner to {process.Name} process.",
                    Href = $"/app/processes/{process.Id}"
                });
            }
        }

        // If no actions, suggest getting started steps
        if (!actions.Any())
        {
            var totalProcesses = await _dbContext.Processes.CountAsync(p => p.OrganizationId == organizationId);
            var totalFunctions = await _dbContext.Functions.CountAsync(f => f.OrganizationId == organizationId);
            var totalRoles = await _dbContext.Roles.CountAsync(r => r.OrganizationId == organizationId);
            var totalPeople = await _dbContext.Resources
                .Include(r => r.ResourceSubtype)
                .CountAsync(r => r.OrganizationId == organizationId && r.ResourceSubtype.ResourceType == ResourceType.Person);

            if (totalProcesses == 0)
                actions.Add(new NextActionDto { Action = "Document your first business process.", Href = "/app/processes" });
            if (totalFunctions == 0)
                actions.Add(new NextActionDto { Action = "Define business functions.", Href = "/app/functions" });
            if (totalRoles == 0)
                actions.Add(new NextActionDto { Action = "Create roles for your organization.", Href = "/app/roles" });
            if (totalPeople == 0)
                actions.Add(new NextActionDto { Action = "Add people to your organization.", Href = "/app/people" });
        }

        return Ok(new DashboardActionsDto
        {
            NextActions = actions.Take(5).ToList()
        });
    }
}

#region DTOs

public class DashboardStatsDto
{
    public int PeopleCount { get; set; }
    public string PeopleChange { get; set; } = "";
    public int RolesCount { get; set; }
    public string RolesChange { get; set; } = "";
    public int FunctionsCount { get; set; }
    public string FunctionsChange { get; set; } = "";
    public int ProcessesCount { get; set; }
    public string ProcessesChange { get; set; } = "";
}

public class DashboardFocusDto
{
    public List<FocusItemDto> FocusItems { get; set; } = new();
}

public class FocusItemDto
{
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Href { get; set; } = "";
}

public class DashboardActionsDto
{
    public List<NextActionDto> NextActions { get; set; } = new();
}

public class NextActionDto
{
    public string Action { get; set; } = "";
    public string? Href { get; set; }
}

#endregion
