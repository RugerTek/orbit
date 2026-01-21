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

        // Soft delete - CLAUDE.md compliance
        role.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    #region Role-Function Assignments

    /// <summary>
    /// Get all functions assigned to a role.
    /// </summary>
    [HttpGet("{roleId}/functions")]
    public async Task<ActionResult<List<RoleFunctionDto>>> GetRoleFunctions(Guid organizationId, Guid roleId)
    {
        // Verify role exists and belongs to this organization
        var roleExists = await _dbContext.Roles
            .AnyAsync(r => r.Id == roleId && r.OrganizationId == organizationId);

        if (!roleExists)
            return NotFound(new { Message = "Role not found" });

        var roleFunctions = await _dbContext.RoleFunctions
            .Where(rf => rf.RoleId == roleId && rf.Role.OrganizationId == organizationId)
            .Select(rf => new RoleFunctionDto
            {
                Id = rf.Id,
                RoleId = rf.RoleId,
                RoleName = rf.Role.Name,
                RoleDepartment = rf.Role.Department,
                FunctionId = rf.FunctionId,
                FunctionName = rf.Function.Name,
                FunctionCategory = rf.Function.Category,
                CreatedAt = rf.CreatedAt
            })
            .OrderBy(rf => rf.FunctionName)
            .ToListAsync();

        return Ok(roleFunctions);
    }

    /// <summary>
    /// Assign a function to a role.
    /// </summary>
    [HttpPost("{roleId}/functions")]
    public async Task<ActionResult<RoleFunctionDto>> AssignFunctionToRole(
        Guid organizationId,
        Guid roleId,
        [FromBody] AssignFunctionToRoleRequest request)
    {
        // Verify role exists and belongs to this organization
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.OrganizationId == organizationId);

        if (role == null)
            return NotFound(new { Message = "Role not found" });

        // Verify function exists and belongs to the same organization
        var function = await _dbContext.Functions
            .FirstOrDefaultAsync(f => f.Id == request.FunctionId && f.OrganizationId == organizationId);

        if (function == null)
            return NotFound(new { Message = "Function not found" });

        // Check if assignment already exists
        var existingAssignment = await _dbContext.RoleFunctions
            .FirstOrDefaultAsync(rf => rf.RoleId == roleId && rf.FunctionId == request.FunctionId);

        if (existingAssignment != null)
            return Conflict(new { Message = "Function is already assigned to this role" });

        var roleFunction = new RoleFunction
        {
            RoleId = roleId,
            FunctionId = request.FunctionId
        };

        _dbContext.RoleFunctions.Add(roleFunction);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRoleFunctions), new { organizationId, roleId },
            new RoleFunctionDto
            {
                Id = roleFunction.Id,
                RoleId = roleFunction.RoleId,
                RoleName = role.Name,
                RoleDepartment = role.Department,
                FunctionId = roleFunction.FunctionId,
                FunctionName = function.Name,
                FunctionCategory = function.Category,
                CreatedAt = roleFunction.CreatedAt
            });
    }

    /// <summary>
    /// Remove a function from a role.
    /// </summary>
    [HttpDelete("{roleId}/functions/{functionId}")]
    public async Task<IActionResult> UnassignFunctionFromRole(Guid organizationId, Guid roleId, Guid functionId)
    {
        // Verify role belongs to this organization
        var roleExists = await _dbContext.Roles
            .AnyAsync(r => r.Id == roleId && r.OrganizationId == organizationId);

        if (!roleExists)
            return NotFound(new { Message = "Role not found" });

        var roleFunction = await _dbContext.RoleFunctions
            .FirstOrDefaultAsync(rf => rf.RoleId == roleId && rf.FunctionId == functionId);

        if (roleFunction == null)
            return NotFound(new { Message = "Function is not assigned to this role" });

        // Soft delete - CLAUDE.md compliance
        roleFunction.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    #endregion
}
