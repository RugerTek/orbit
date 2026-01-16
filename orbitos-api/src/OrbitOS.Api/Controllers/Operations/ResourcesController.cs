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

        _dbContext.ResourceSubtypes.Remove(subtype);
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

        _dbContext.Resources.Remove(resource);
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

        _dbContext.RoleAssignments.Remove(assignment);
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

        var capability = new FunctionCapability
        {
            ResourceId = request.ResourceId,
            FunctionId = request.FunctionId,
            Level = request.Level,
            CertifiedDate = request.CertifiedDate,
            ExpiresAt = request.ExpiresAt,
            Notes = request.Notes
        };

        _dbContext.FunctionCapabilities.Add(capability);
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

    [HttpDelete("function-capabilities/{id}")]
    public async Task<IActionResult> DeleteFunctionCapability(Guid organizationId, Guid id)
    {
        var capability = await _dbContext.FunctionCapabilities
            .Include(fc => fc.Resource)
            .FirstOrDefaultAsync(fc => fc.Id == id && fc.Resource.OrganizationId == organizationId);

        if (capability == null)
            return NotFound();

        _dbContext.FunctionCapabilities.Remove(capability);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    #endregion
}
