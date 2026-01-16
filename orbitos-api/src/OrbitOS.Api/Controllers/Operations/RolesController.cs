using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages operational roles (job positions/titles) within an organization.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Roles")]
public class RolesController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<RolesController> _logger;

    public RolesController(OrbitOSDbContext dbContext, ILogger<RolesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<OpsRoleDto>>> GetRoles(Guid organizationId)
    {
        var roles = await _dbContext.Roles
            .Where(r => r.OrganizationId == organizationId)
            .Select(r => new OpsRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Purpose = r.Purpose,
                Department = r.Department,
                OrganizationId = r.OrganizationId,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                AssignmentCount = _dbContext.RoleAssignments.Count(ra => ra.RoleId == r.Id),
                FunctionCount = r.RoleFunctions.Count
            })
            .OrderBy(r => r.Name)
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OpsRoleDto>> GetRole(Guid organizationId, Guid id)
    {
        var role = await _dbContext.Roles
            .Where(r => r.Id == id && r.OrganizationId == organizationId)
            .Select(r => new OpsRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Purpose = r.Purpose,
                Department = r.Department,
                OrganizationId = r.OrganizationId,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                AssignmentCount = _dbContext.RoleAssignments.Count(ra => ra.RoleId == r.Id),
                FunctionCount = r.RoleFunctions.Count
            })
            .FirstOrDefaultAsync();

        if (role == null)
            return NotFound();

        return Ok(role);
    }

    [HttpPost]
    public async Task<ActionResult<OpsRoleDto>> CreateRole(
        Guid organizationId,
        [FromBody] CreateOpsRoleRequest request)
    {
        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            Purpose = request.Purpose,
            Department = request.Department,
            OrganizationId = organizationId
        };

        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRole), new { organizationId, id = role.Id },
            new OpsRoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Purpose = role.Purpose,
                Department = role.Department,
                OrganizationId = role.OrganizationId,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                AssignmentCount = 0,
                FunctionCount = 0
            });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OpsRoleDto>> UpdateRole(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateOpsRoleRequest request)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == organizationId);

        if (role == null)
            return NotFound();

        role.Name = request.Name;
        role.Description = request.Description;
        role.Purpose = request.Purpose;
        role.Department = request.Department;

        await _dbContext.SaveChangesAsync();

        return Ok(new OpsRoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Purpose = role.Purpose,
            Department = role.Department,
            OrganizationId = role.OrganizationId,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(Guid organizationId, Guid id)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == organizationId);

        if (role == null)
            return NotFound();

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
