using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages resources (people, equipment, etc.) and their subtypes.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Resources")]
public class ResourcesController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<ResourcesController> _logger;

    public ResourcesController(OrbitOSDbContext dbContext, ILogger<ResourcesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    #region Resource Subtypes

    [HttpGet("subtypes")]
    public async Task<ActionResult<List<ResourceSubtypeDto>>> GetResourceSubtypes(Guid organizationId)
    {
        var subtypes = await _dbContext.ResourceSubtypes
            .Where(s => s.OrganizationId == organizationId)
            .Select(s => new ResourceSubtypeDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                ResourceType = s.ResourceType,
                Icon = s.Icon,
                OrganizationId = s.OrganizationId,
                CreatedAt = s.CreatedAt,
                ResourceCount = s.Resources.Count
            })
            .OrderBy(s => s.ResourceType)
            .ThenBy(s => s.Name)
            .ToListAsync();

        return Ok(subtypes);
    }

    [HttpPost("subtypes")]
    public async Task<ActionResult<ResourceSubtypeDto>> CreateResourceSubtype(
        Guid organizationId,
        [FromBody] CreateResourceSubtypeRequest request)
    {
        var subtype = new ResourceSubtype
        {
            Name = request.Name,
            Description = request.Description,
            ResourceType = request.ResourceType,
            Icon = request.Icon,
            OrganizationId = organizationId
        };

        _dbContext.ResourceSubtypes.Add(subtype);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetResourceSubtypes), new { organizationId },
            new ResourceSubtypeDto
            {
                Id = subtype.Id,
                Name = subtype.Name,
                Description = subtype.Description,
                ResourceType = subtype.ResourceType,
                Icon = subtype.Icon,
                OrganizationId = subtype.OrganizationId,
                CreatedAt = subtype.CreatedAt,
                ResourceCount = 0
            });
    }

    [HttpDelete("subtypes/{id}")]
    public async Task<IActionResult> DeleteResourceSubtype(Guid organizationId, Guid id)
    {
        var subtype = await _dbContext.ResourceSubtypes
            .FirstOrDefaultAsync(s => s.Id == id && s.OrganizationId == organizationId);

        if (subtype == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        subtype.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Resources

    [HttpGet]
    public async Task<ActionResult<List<ResourceDto>>> GetResources(Guid organizationId, [FromQuery] ResourceType? type = null)
    {
        var query = _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .Include(r => r.LinkedUser)
            .Where(r => r.OrganizationId == organizationId);

        if (type.HasValue)
        {
            query = query.Where(r => r.ResourceSubtype.ResourceType == type.Value);
        }

        var resources = await query
            .Select(r => new ResourceDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Status = r.Status,
                Metadata = r.Metadata,
                OrganizationId = r.OrganizationId,
                ResourceSubtypeId = r.ResourceSubtypeId,
                ResourceSubtypeName = r.ResourceSubtype.Name,
                ResourceType = r.ResourceSubtype.ResourceType,
                LinkedUserId = r.LinkedUserId,
                LinkedUserName = r.LinkedUser != null ? r.LinkedUser.DisplayName : null,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                RoleAssignmentCount = r.RoleAssignments.Count,
                FunctionCapabilityCount = r.FunctionCapabilities.Count
            })
            .OrderBy(r => r.Name)
            .ToListAsync();

        return Ok(resources);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResourceDto>> GetResource(Guid organizationId, Guid id)
    {
        var resource = await _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .Include(r => r.LinkedUser)
            .Where(r => r.Id == id && r.OrganizationId == organizationId)
            .Select(r => new ResourceDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Status = r.Status,
                Metadata = r.Metadata,
                OrganizationId = r.OrganizationId,
                ResourceSubtypeId = r.ResourceSubtypeId,
                ResourceSubtypeName = r.ResourceSubtype.Name,
                ResourceType = r.ResourceSubtype.ResourceType,
                LinkedUserId = r.LinkedUserId,
                LinkedUserName = r.LinkedUser != null ? r.LinkedUser.DisplayName : null,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                RoleAssignmentCount = r.RoleAssignments.Count,
                FunctionCapabilityCount = r.FunctionCapabilities.Count
            })
            .FirstOrDefaultAsync();

        if (resource == null)
            return NotFound();

        return Ok(resource);
    }

    [HttpPost]
    public async Task<ActionResult<ResourceDto>> CreateResource(
        Guid organizationId,
        [FromBody] CreateResourceRequest request)
    {
        var subtype = await _dbContext.ResourceSubtypes
            .FirstOrDefaultAsync(s => s.Id == request.ResourceSubtypeId && s.OrganizationId == organizationId);

        if (subtype == null)
            return BadRequest("Invalid resource subtype");

        var resource = new Resource
        {
            Name = request.Name,
            Description = request.Description,
            Status = request.Status,
            Metadata = request.Metadata,
            OrganizationId = organizationId,
            ResourceSubtypeId = request.ResourceSubtypeId,
            LinkedUserId = request.LinkedUserId
        };

        _dbContext.Resources.Add(resource);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetResource), new { organizationId, id = resource.Id },
            new ResourceDto
            {
                Id = resource.Id,
                Name = resource.Name,
                Description = resource.Description,
                Status = resource.Status,
                Metadata = resource.Metadata,
                OrganizationId = resource.OrganizationId,
                ResourceSubtypeId = resource.ResourceSubtypeId,
                ResourceSubtypeName = subtype.Name,
                ResourceType = subtype.ResourceType,
                LinkedUserId = resource.LinkedUserId,
                CreatedAt = resource.CreatedAt,
                UpdatedAt = resource.UpdatedAt,
                RoleAssignmentCount = 0,
                FunctionCapabilityCount = 0
            });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ResourceDto>> UpdateResource(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateResourceRequest request)
    {
        var resource = await _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == organizationId);

        if (resource == null)
            return NotFound();

        resource.Name = request.Name;
        resource.Description = request.Description;
        resource.Status = request.Status;
        resource.Metadata = request.Metadata;
        resource.LinkedUserId = request.LinkedUserId;

        await _dbContext.SaveChangesAsync();

        return Ok(new ResourceDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Description = resource.Description,
            Status = resource.Status,
            Metadata = resource.Metadata,
            OrganizationId = resource.OrganizationId,
            ResourceSubtypeId = resource.ResourceSubtypeId,
            ResourceSubtypeName = resource.ResourceSubtype.Name,
            ResourceType = resource.ResourceSubtype.ResourceType,
            LinkedUserId = resource.LinkedUserId,
            CreatedAt = resource.CreatedAt,
            UpdatedAt = resource.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResource(Guid organizationId, Guid id)
    {
        var resource = await _dbContext.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == organizationId);

        if (resource == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        resource.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Role Assignments

    [HttpGet("role-assignments")]
    public async Task<ActionResult<List<RoleAssignmentDto>>> GetRoleAssignments(Guid organizationId, [FromQuery] Guid? resourceId = null)
    {
        var query = _dbContext.RoleAssignments
            .Include(ra => ra.Resource)
            .Include(ra => ra.Role)
            .Where(ra => ra.Resource.OrganizationId == organizationId);

        if (resourceId.HasValue)
        {
            query = query.Where(ra => ra.ResourceId == resourceId.Value);
        }

        var assignments = await query
            .Select(ra => new RoleAssignmentDto
            {
                Id = ra.Id,
                ResourceId = ra.ResourceId,
                ResourceName = ra.Resource.Name,
                RoleId = ra.RoleId,
                RoleName = ra.Role.Name,
                AllocationPercentage = ra.AllocationPercentage,
                IsPrimary = ra.IsPrimary,
                StartDate = ra.StartDate,
                EndDate = ra.EndDate,
                CreatedAt = ra.CreatedAt
            })
            .ToListAsync();

        return Ok(assignments);
    }

    [HttpPost("role-assignments")]
    public async Task<ActionResult<RoleAssignmentDto>> CreateRoleAssignment(
        Guid organizationId,
        [FromBody] CreateRoleAssignmentRequest request)
    {
        var resource = await _dbContext.Resources
            .FirstOrDefaultAsync(r => r.Id == request.ResourceId && r.OrganizationId == organizationId);

        if (resource == null)
            return BadRequest("Invalid resource");

        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.OrganizationId == organizationId);

        if (role == null)
            return BadRequest("Invalid role");

        var assignment = new RoleAssignment
        {
            ResourceId = request.ResourceId,
            RoleId = request.RoleId,
            AllocationPercentage = request.AllocationPercentage,
            IsPrimary = request.IsPrimary,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _dbContext.RoleAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRoleAssignments), new { organizationId },
            new RoleAssignmentDto
            {
                Id = assignment.Id,
                ResourceId = assignment.ResourceId,
                ResourceName = resource.Name,
                RoleId = assignment.RoleId,
                RoleName = role.Name,
                AllocationPercentage = assignment.AllocationPercentage,
                IsPrimary = assignment.IsPrimary,
                StartDate = assignment.StartDate,
                EndDate = assignment.EndDate,
                CreatedAt = assignment.CreatedAt
            });
    }

    [HttpDelete("role-assignments/{id}")]
    public async Task<IActionResult> DeleteRoleAssignment(Guid organizationId, Guid id)
    {
        var assignment = await _dbContext.RoleAssignments
            .Include(ra => ra.Resource)
            .FirstOrDefaultAsync(ra => ra.Id == id && ra.Resource.OrganizationId == organizationId);

        if (assignment == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        assignment.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Function Capabilities

    [HttpGet("function-capabilities")]
    public async Task<ActionResult<List<FunctionCapabilityDto>>> GetFunctionCapabilities(Guid organizationId, [FromQuery] Guid? resourceId = null)
    {
        var query = _dbContext.FunctionCapabilities
            .Include(fc => fc.Resource)
            .Include(fc => fc.Function)
            .Where(fc => fc.Resource.OrganizationId == organizationId);

        if (resourceId.HasValue)
        {
            query = query.Where(fc => fc.ResourceId == resourceId.Value);
        }

        var capabilities = await query
            .Select(fc => new FunctionCapabilityDto
            {
                Id = fc.Id,
                ResourceId = fc.ResourceId,
                ResourceName = fc.Resource.Name,
                FunctionId = fc.FunctionId,
                FunctionName = fc.Function.Name,
                Level = fc.Level,
                CertifiedDate = fc.CertifiedDate,
                ExpiresAt = fc.ExpiresAt,
                Notes = fc.Notes,
                CreatedAt = fc.CreatedAt
            })
            .ToListAsync();

        return Ok(capabilities);
    }

    [HttpPost("function-capabilities")]
    public async Task<ActionResult<FunctionCapabilityDto>> CreateFunctionCapability(
        Guid organizationId,
        [FromBody] CreateFunctionCapabilityRequest request)
    {
        var resource = await _dbContext.Resources
            .FirstOrDefaultAsync(r => r.Id == request.ResourceId && r.OrganizationId == organizationId);

        if (resource == null)
            return BadRequest("Invalid resource");

        var function = await _dbContext.Functions
            .FirstOrDefaultAsync(f => f.Id == request.FunctionId && f.OrganizationId == organizationId);

        if (function == null)
            return BadRequest("Invalid function");

        // Check for soft-deleted existing record and undelete it instead of creating new
        var existingSoftDeleted = await _dbContext.FunctionCapabilities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(fc => fc.ResourceId == request.ResourceId
                && fc.FunctionId == request.FunctionId
                && fc.DeletedAt != null);

        FunctionCapability capability;
        if (existingSoftDeleted != null)
        {
            // Restore the soft-deleted record with new values
            existingSoftDeleted.DeletedAt = null;
            existingSoftDeleted.Level = request.Level;
            existingSoftDeleted.CertifiedDate = request.CertifiedDate;
            existingSoftDeleted.ExpiresAt = request.ExpiresAt;
            existingSoftDeleted.Notes = request.Notes;
            capability = existingSoftDeleted;
        }
        else
        {
            capability = new FunctionCapability
            {
                ResourceId = request.ResourceId,
                FunctionId = request.FunctionId,
                Level = request.Level,
                CertifiedDate = request.CertifiedDate,
                ExpiresAt = request.ExpiresAt,
                Notes = request.Notes
            };
            _dbContext.FunctionCapabilities.Add(capability);
        }

        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFunctionCapabilities), new { organizationId },
            new FunctionCapabilityDto
            {
                Id = capability.Id,
                ResourceId = capability.ResourceId,
                ResourceName = resource.Name,
                FunctionId = capability.FunctionId,
                FunctionName = function.Name,
                Level = capability.Level,
                CertifiedDate = capability.CertifiedDate,
                ExpiresAt = capability.ExpiresAt,
                Notes = capability.Notes,
                CreatedAt = capability.CreatedAt
            });
    }

    [HttpPut("function-capabilities/{id}")]
    public async Task<ActionResult<FunctionCapabilityDto>> UpdateFunctionCapability(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateFunctionCapabilityRequest request)
    {
        var capability = await _dbContext.FunctionCapabilities
            .Include(fc => fc.Resource)
            .Include(fc => fc.Function)
            .FirstOrDefaultAsync(fc => fc.Id == id && fc.Resource.OrganizationId == organizationId);

        if (capability == null)
            return NotFound();

        capability.Level = request.Level;
        capability.CertifiedDate = request.CertifiedDate;
        capability.ExpiresAt = request.ExpiresAt;
        capability.Notes = request.Notes;

        await _dbContext.SaveChangesAsync();

        return Ok(new FunctionCapabilityDto
        {
            Id = capability.Id,
            ResourceId = capability.ResourceId,
            ResourceName = capability.Resource.Name,
            FunctionId = capability.FunctionId,
            FunctionName = capability.Function.Name,
            Level = capability.Level,
            CertifiedDate = capability.CertifiedDate,
            ExpiresAt = capability.ExpiresAt,
            Notes = capability.Notes,
            CreatedAt = capability.CreatedAt
        });
    }

    [HttpDelete("function-capabilities/{id}")]
    public async Task<IActionResult> DeleteFunctionCapability(Guid organizationId, Guid id)
    {
        var capability = await _dbContext.FunctionCapabilities
            .Include(fc => fc.Resource)
            .FirstOrDefaultAsync(fc => fc.Id == id && fc.Resource.OrganizationId == organizationId);

        if (capability == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        capability.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Org Chart

    /// <summary>
    /// Get the full organizational chart tree structure (people hierarchy)
    /// </summary>
    [HttpGet("org-chart")]
    public async Task<ActionResult<OrgChartTreeDto>> GetOrgChart(Guid organizationId)
    {
        // Get all person-type resources with their reporting relationships
        var people = await _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .Include(r => r.LinkedUser)
            .Include(r => r.ReportsToResource)
            .Include(r => r.DirectReports)
            .Where(r => r.OrganizationId == organizationId)
            .Where(r => r.ResourceSubtype.ResourceType == ResourceType.Person)
            .ToListAsync();

        // Build lookup maps
        var peopleById = people.ToDictionary(p => p.Id);
        var childrenMap = people
            .Where(p => p.ReportsToResourceId.HasValue)
            .GroupBy(p => p.ReportsToResourceId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Calculate indirect reports count recursively
        int CountIndirectReports(Guid resourceId)
        {
            if (!childrenMap.TryGetValue(resourceId, out var children))
                return 0;

            int count = children.Count;
            foreach (var child in children)
            {
                count += CountIndirectReports(child.Id);
            }
            return count;
        }

        // Calculate management depth (distance from root)
        int CalculateDepth(Resource resource)
        {
            int depth = 0;
            var current = resource;
            while (current.ReportsToResourceId.HasValue &&
                   peopleById.TryGetValue(current.ReportsToResourceId.Value, out var manager))
            {
                depth++;
                current = manager;
            }
            return depth;
        }

        // Convert to DTOs with computed fields
        OrgChartResourceDto ToDto(Resource r, int depth)
        {
            var directReportsCount = childrenMap.TryGetValue(r.Id, out var children) ? children.Count : 0;
            return new OrgChartResourceDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Status = r.Status,
                OrganizationId = r.OrganizationId,
                ResourceSubtypeId = r.ResourceSubtypeId,
                ResourceSubtypeName = r.ResourceSubtype.Name,
                ResourceType = r.ResourceSubtype.ResourceType,
                LinkedUserId = r.LinkedUserId,
                LinkedUserName = r.LinkedUser?.DisplayName,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                ReportsToResourceId = r.ReportsToResourceId,
                ManagerName = r.ReportsToResource?.Name,
                IsVacant = r.IsVacant,
                VacantPositionTitle = r.VacantPositionTitle,
                DirectReportsCount = directReportsCount,
                IndirectReportsCount = CountIndirectReports(r.Id),
                ManagementDepth = depth
            };
        }

        // Build tree recursively
        List<OrgChartResourceDto> BuildSubtree(Guid? parentId, int depth)
        {
            var nodes = people
                .Where(p => p.ReportsToResourceId == parentId)
                .Select(p =>
                {
                    var dto = ToDto(p, depth);
                    dto.DirectReports = BuildSubtree(p.Id, depth + 1);
                    return dto;
                })
                .OrderBy(d => d.Name)
                .ToList();
            return nodes;
        }

        var rootNodes = BuildSubtree(null, 0);

        // Calculate metrics
        var peopleByDepth = people
            .GroupBy(p => CalculateDepth(p))
            .ToDictionary(g => g.Key, g => g.Count());

        var maxDepth = peopleByDepth.Keys.Any() ? peopleByDepth.Keys.Max() : 0;

        return Ok(new OrgChartTreeDto
        {
            RootNodes = rootNodes,
            TotalPeople = people.Count(p => !p.IsVacant),
            TotalVacancies = people.Count(p => p.IsVacant),
            MaxDepth = maxDepth,
            PeopleByDepth = peopleByDepth
        });
    }

    /// <summary>
    /// Get organizational chart metrics (span of control, vacancies, depth)
    /// </summary>
    [HttpGet("org-chart/metrics")]
    public async Task<ActionResult<OrgChartMetricsDto>> GetOrgChartMetrics(Guid organizationId)
    {
        var people = await _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .Include(r => r.DirectReports)
            .Where(r => r.OrganizationId == organizationId)
            .Where(r => r.ResourceSubtype.ResourceType == ResourceType.Person)
            .ToListAsync();

        var peopleById = people.ToDictionary(p => p.Id);
        var childrenMap = people
            .Where(p => p.ReportsToResourceId.HasValue)
            .GroupBy(p => p.ReportsToResourceId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        int CountIndirectReports(Guid resourceId)
        {
            if (!childrenMap.TryGetValue(resourceId, out var children))
                return 0;
            return children.Count + children.Sum(c => CountIndirectReports(c.Id));
        }

        int CalculateDepth(Resource resource)
        {
            int depth = 0;
            var current = resource;
            while (current.ReportsToResourceId.HasValue &&
                   peopleById.TryGetValue(current.ReportsToResourceId.Value, out var manager))
            {
                depth++;
                current = manager;
            }
            return depth;
        }

        // Managers are people who have at least one direct report
        var managers = people.Where(p => childrenMap.ContainsKey(p.Id)).ToList();

        var spanOfControlEntries = managers
            .Select(m => new SpanOfControlEntry
            {
                ManagerId = m.Id,
                ManagerName = m.Name,
                DirectReports = childrenMap[m.Id].Count,
                IndirectReports = CountIndirectReports(m.Id),
                Depth = CalculateDepth(m)
            })
            .OrderByDescending(e => e.DirectReports)
            .ToList();

        var avgSpan = managers.Count > 0
            ? (decimal)managers.Sum(m => childrenMap[m.Id].Count) / managers.Count
            : 0;

        var maxDepth = people.Count > 0 ? people.Max(p => CalculateDepth(p)) : 0;

        return Ok(new OrgChartMetricsDto
        {
            TotalPeople = people.Count(p => !p.IsVacant),
            TotalVacancies = people.Count(p => p.IsVacant),
            MaxDepth = maxDepth,
            AverageSpanOfControl = Math.Round(avgSpan, 2),
            SpanOfControlByManager = spanOfControlEntries
        });
    }

    /// <summary>
    /// Update a person's reporting relationship (change manager)
    /// </summary>
    [HttpPatch("{id}/reporting")]
    public async Task<ActionResult<OrgChartResourceDto>> UpdateReporting(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateReportingRequest request)
    {
        var resource = await _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .Include(r => r.LinkedUser)
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == organizationId);

        if (resource == null)
            return NotFound();

        if (resource.ResourceSubtype.ResourceType != ResourceType.Person)
            return BadRequest("Only person resources can have reporting relationships");

        // Validate: cannot report to self
        if (request.ReportsToResourceId == id)
            return BadRequest(new { error = "RESOURCE_SELF_REPORT", message = "A person cannot report to themselves" });

        // Validate manager exists and is a person in same org
        if (request.ReportsToResourceId.HasValue)
        {
            var manager = await _dbContext.Resources
                .Include(r => r.ResourceSubtype)
                .FirstOrDefaultAsync(r => r.Id == request.ReportsToResourceId.Value && r.OrganizationId == organizationId);

            if (manager == null)
                return BadRequest(new { error = "RESOURCE_INVALID_MANAGER", message = "Manager not found in this organization" });

            if (manager.ResourceSubtype.ResourceType != ResourceType.Person)
                return BadRequest(new { error = "RESOURCE_INVALID_MANAGER", message = "Manager must be a person" });

            // Check for circular reference
            if (await WouldCreateCircularReference(id, request.ReportsToResourceId.Value, organizationId))
                return BadRequest(new { error = "RESOURCE_CIRCULAR_REPORT", message = "This would create a circular reporting relationship" });
        }

        resource.ReportsToResourceId = request.ReportsToResourceId;
        await _dbContext.SaveChangesAsync();

        // Reload with manager info
        await _dbContext.Entry(resource).Reference(r => r.ReportsToResource).LoadAsync();

        var directReportsCount = await _dbContext.Resources
            .CountAsync(r => r.ReportsToResourceId == id);

        return Ok(new OrgChartResourceDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Description = resource.Description,
            Status = resource.Status,
            OrganizationId = resource.OrganizationId,
            ResourceSubtypeId = resource.ResourceSubtypeId,
            ResourceSubtypeName = resource.ResourceSubtype.Name,
            ResourceType = resource.ResourceSubtype.ResourceType,
            LinkedUserId = resource.LinkedUserId,
            LinkedUserName = resource.LinkedUser?.DisplayName,
            CreatedAt = resource.CreatedAt,
            UpdatedAt = resource.UpdatedAt,
            ReportsToResourceId = resource.ReportsToResourceId,
            ManagerName = resource.ReportsToResource?.Name,
            IsVacant = resource.IsVacant,
            VacantPositionTitle = resource.VacantPositionTitle,
            DirectReportsCount = directReportsCount
        });
    }

    /// <summary>
    /// Create a vacant position in the org chart
    /// </summary>
    [HttpPost("vacancies")]
    public async Task<ActionResult<OrgChartResourceDto>> CreateVacancy(
        Guid organizationId,
        [FromBody] CreateVacantPositionRequest request)
    {
        // Validate subtype exists and is Person type
        var subtype = await _dbContext.ResourceSubtypes
            .FirstOrDefaultAsync(s => s.Id == request.ResourceSubtypeId && s.OrganizationId == organizationId);

        if (subtype == null)
            return BadRequest("Invalid resource subtype");

        if (subtype.ResourceType != ResourceType.Person)
            return BadRequest("Only person subtypes can be used for vacant positions");

        // Validate manager if specified
        if (request.ReportsToResourceId.HasValue)
        {
            var manager = await _dbContext.Resources
                .Include(r => r.ResourceSubtype)
                .FirstOrDefaultAsync(r => r.Id == request.ReportsToResourceId.Value && r.OrganizationId == organizationId);

            if (manager == null)
                return BadRequest("Manager not found");

            if (manager.ResourceSubtype.ResourceType != ResourceType.Person)
                return BadRequest("Manager must be a person");
        }

        var vacancy = new Resource
        {
            Name = $"[Vacant] {request.VacantPositionTitle}",
            Description = request.Description,
            Status = ResourceStatus.Planned,
            OrganizationId = organizationId,
            ResourceSubtypeId = request.ResourceSubtypeId,
            IsVacant = true,
            VacantPositionTitle = request.VacantPositionTitle,
            ReportsToResourceId = request.ReportsToResourceId
        };

        _dbContext.Resources.Add(vacancy);
        await _dbContext.SaveChangesAsync();

        // Load manager name if exists
        Resource? manager2 = null;
        if (vacancy.ReportsToResourceId.HasValue)
        {
            manager2 = await _dbContext.Resources.FindAsync(vacancy.ReportsToResourceId.Value);
        }

        return CreatedAtAction(nameof(GetResource), new { organizationId, id = vacancy.Id },
            new OrgChartResourceDto
            {
                Id = vacancy.Id,
                Name = vacancy.Name,
                Description = vacancy.Description,
                Status = vacancy.Status,
                OrganizationId = vacancy.OrganizationId,
                ResourceSubtypeId = vacancy.ResourceSubtypeId,
                ResourceSubtypeName = subtype.Name,
                ResourceType = subtype.ResourceType,
                CreatedAt = vacancy.CreatedAt,
                UpdatedAt = vacancy.UpdatedAt,
                ReportsToResourceId = vacancy.ReportsToResourceId,
                ManagerName = manager2?.Name,
                IsVacant = true,
                VacantPositionTitle = vacancy.VacantPositionTitle,
                DirectReportsCount = 0
            });
    }

    /// <summary>
    /// Fill a vacant position with person details
    /// </summary>
    [HttpPost("vacancies/{id}/fill")]
    public async Task<ActionResult<OrgChartResourceDto>> FillVacancy(
        Guid organizationId,
        Guid id,
        [FromBody] FillVacancyRequest request)
    {
        var vacancy = await _dbContext.Resources
            .Include(r => r.ResourceSubtype)
            .Include(r => r.ReportsToResource)
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == organizationId);

        if (vacancy == null)
            return NotFound();

        if (!vacancy.IsVacant)
            return BadRequest("This position is not vacant");

        // Fill the vacancy
        vacancy.Name = request.Name;
        vacancy.Description = request.Description;
        vacancy.LinkedUserId = request.LinkedUserId;
        vacancy.IsVacant = false;
        vacancy.Status = ResourceStatus.Active;

        await _dbContext.SaveChangesAsync();

        // Load linked user if exists
        if (vacancy.LinkedUserId.HasValue)
        {
            await _dbContext.Entry(vacancy).Reference(r => r.LinkedUser).LoadAsync();
        }

        var directReportsCount = await _dbContext.Resources
            .CountAsync(r => r.ReportsToResourceId == id);

        return Ok(new OrgChartResourceDto
        {
            Id = vacancy.Id,
            Name = vacancy.Name,
            Description = vacancy.Description,
            Status = vacancy.Status,
            OrganizationId = vacancy.OrganizationId,
            ResourceSubtypeId = vacancy.ResourceSubtypeId,
            ResourceSubtypeName = vacancy.ResourceSubtype.Name,
            ResourceType = vacancy.ResourceSubtype.ResourceType,
            LinkedUserId = vacancy.LinkedUserId,
            LinkedUserName = vacancy.LinkedUser?.DisplayName,
            CreatedAt = vacancy.CreatedAt,
            UpdatedAt = vacancy.UpdatedAt,
            ReportsToResourceId = vacancy.ReportsToResourceId,
            ManagerName = vacancy.ReportsToResource?.Name,
            IsVacant = false,
            VacantPositionTitle = null,
            DirectReportsCount = directReportsCount
        });
    }

    /// <summary>
    /// Check if setting a new manager would create a circular reference
    /// </summary>
    private async Task<bool> WouldCreateCircularReference(Guid resourceId, Guid newManagerId, Guid organizationId)
    {
        if (resourceId == newManagerId)
            return true;

        var visited = new HashSet<Guid> { resourceId };
        var currentId = newManagerId;

        // Walk up the chain from the proposed new manager
        while (true)
        {
            if (visited.Contains(currentId))
                return true;

            visited.Add(currentId);

            var managerId = await _dbContext.Resources
                .Where(r => r.Id == currentId && r.OrganizationId == organizationId)
                .Select(r => r.ReportsToResourceId)
                .FirstOrDefaultAsync();

            if (!managerId.HasValue)
                break;

            currentId = managerId.Value;
        }

        return false;
    }

    #endregion
}
