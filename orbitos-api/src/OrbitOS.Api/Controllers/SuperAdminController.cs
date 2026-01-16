using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers;

// DTOs for Super Admin operations
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool HasPassword { get; set; }
    public bool HasGoogleId { get; set; }
    public bool HasAzureAdId { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int OrganizationCount { get; set; }
}

public class CreateUserRequest
{
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Password { get; set; }
}

public class UpdateUserRequest
{
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? AzureAdTenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MemberCount { get; set; }
    public int RoleCount { get; set; }
    public int FunctionCount { get; set; }
}

public class CreateOrganizationRequest
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
}

public class UpdateOrganizationRequest
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
}

// System Role DTOs (platform access control)
public class SystemRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsBuiltIn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int PermissionCount { get; set; }
    public int UserCount { get; set; }
}

public class CreateSystemRoleRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateSystemRoleRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

// Permission DTOs (platform permissions)
public class PermissionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Category { get; set; } = "";
    public bool IsBuiltIn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int SystemRoleCount { get; set; }
}

public class CreatePermissionRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
}

public class UpdatePermissionRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
}

public class OrganizationMembershipDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = "";
    public string UserDisplayName { get; set; } = "";
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = "";
    public MembershipRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddMemberRequest
{
    public required Guid UserId { get; set; }
    public required Guid OrganizationId { get; set; }
    public MembershipRole Role { get; set; } = MembershipRole.Member;
}

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalOrganizations { get; set; }
    public int TotalSystemRoles { get; set; }
    public int TotalPermissions { get; set; }
    public int UsersLast30Days { get; set; }
    public int OrgsLast30Days { get; set; }
    public List<RecentActivityDto> RecentActivity { get; set; } = new();
}

public class RecentActivityDto
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class SuperAdminController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<SuperAdminController> _logger;

    public SuperAdminController(OrbitOSDbContext dbContext, ILogger<SuperAdminController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Dashboard Stats
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var stats = new DashboardStatsDto
        {
            TotalUsers = await _dbContext.Users.CountAsync(),
            TotalOrganizations = await _dbContext.Organizations.CountAsync(),
            TotalSystemRoles = await _dbContext.SystemRoles.CountAsync(),
            TotalPermissions = await _dbContext.Permissions.CountAsync(),
            UsersLast30Days = await _dbContext.Users.CountAsync(u => u.CreatedAt >= thirtyDaysAgo),
            OrgsLast30Days = await _dbContext.Organizations.CountAsync(o => o.CreatedAt >= thirtyDaysAgo),
        };

        // Recent activity - last 10 items
        var recentUsers = await _dbContext.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new RecentActivityDto
            {
                Type = "User",
                Description = $"User '{u.DisplayName}' created",
                Timestamp = u.CreatedAt
            })
            .ToListAsync();

        var recentOrgs = await _dbContext.Organizations
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new RecentActivityDto
            {
                Type = "Organization",
                Description = $"Organization '{o.Name}' created",
                Timestamp = o.CreatedAt
            })
            .ToListAsync();

        stats.RecentActivity = recentUsers
            .Concat(recentOrgs)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToList();

        return Ok(stats);
    }

    // ==================== USERS ====================

    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetUsers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(search) || u.DisplayName.ToLower().Contains(search));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                AvatarUrl = u.AvatarUrl,
                HasPassword = u.PasswordHash != null,
                HasGoogleId = u.GoogleId != null,
                HasAzureAdId = u.AzureAdObjectId != null,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                OrganizationCount = u.OrganizationMemberships.Count
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("users/count")]
    public async Task<ActionResult<int>> GetUsersCount([FromQuery] string? search)
    {
        var query = _dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(search) || u.DisplayName.ToLower().Contains(search));
        }

        return Ok(await query.CountAsync());
    }

    [HttpGet("users/{id}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var user = await _dbContext.Users
            .Include(u => u.OrganizationMemberships)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            HasPassword = user.PasswordHash != null,
            HasGoogleId = user.GoogleId != null,
            HasAzureAdId = user.AzureAdObjectId != null,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            OrganizationCount = user.OrganizationMemberships.Count
        });
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            return BadRequest(new { Message = "A user with this email already exists" });
        }

        var user = new User
        {
            Email = request.Email,
            DisplayName = request.DisplayName,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = HashPassword(request.Password);
        }

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin created user: {Email}", user.Email);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            HasPassword = user.PasswordHash != null,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    [HttpPut("users/{id}")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Check email uniqueness if changed
        if (user.Email.ToLower() != request.Email.ToLower())
        {
            if (await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != id))
            {
                return BadRequest(new { Message = "A user with this email already exists" });
            }
        }

        user.Email = request.Email;
        user.DisplayName = request.DisplayName;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin updated user: {Email}", user.Email);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            HasPassword = user.PasswordHash != null,
            HasGoogleId = user.GoogleId != null,
            HasAzureAdId = user.AzureAdObjectId != null,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin deleted user: {Email}", user.Email);

        return NoContent();
    }

    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(Guid id, [FromBody] ResetPasswordRequest request)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.PasswordHash = HashPassword(request.NewPassword);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin reset password for user: {Email}", user.Email);

        return Ok(new { Message = "Password reset successfully" });
    }

    // ==================== ORGANIZATIONS ====================

    [HttpGet("organizations")]
    public async Task<ActionResult<List<OrganizationDto>>> GetOrganizations([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _dbContext.Organizations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(o => o.Name.ToLower().Contains(search) || o.Slug.ToLower().Contains(search));
        }

        var orgs = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrganizationDto
            {
                Id = o.Id,
                Name = o.Name,
                Slug = o.Slug,
                Description = o.Description,
                LogoUrl = o.LogoUrl,
                AzureAdTenantId = o.AzureAdTenantId,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                MemberCount = o.Memberships.Count,
                RoleCount = o.Roles.Count,
                FunctionCount = o.Functions.Count
            })
            .ToListAsync();

        return Ok(orgs);
    }

    [HttpGet("organizations/count")]
    public async Task<ActionResult<int>> GetOrganizationsCount([FromQuery] string? search)
    {
        var query = _dbContext.Organizations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(o => o.Name.ToLower().Contains(search) || o.Slug.ToLower().Contains(search));
        }

        return Ok(await query.CountAsync());
    }

    [HttpGet("organizations/{id}")]
    public async Task<ActionResult<OrganizationDto>> GetOrganization(Guid id)
    {
        var org = await _dbContext.Organizations
            .Include(o => o.Memberships)
            .Include(o => o.Roles)
            .Include(o => o.Functions)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (org == null) return NotFound();

        return Ok(new OrganizationDto
        {
            Id = org.Id,
            Name = org.Name,
            Slug = org.Slug,
            Description = org.Description,
            LogoUrl = org.LogoUrl,
            AzureAdTenantId = org.AzureAdTenantId,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt,
            MemberCount = org.Memberships.Count,
            RoleCount = org.Roles.Count,
            FunctionCount = org.Functions.Count
        });
    }

    [HttpPost("organizations")]
    public async Task<ActionResult<OrganizationDto>> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        if (await _dbContext.Organizations.AnyAsync(o => o.Slug.ToLower() == request.Slug.ToLower()))
        {
            return BadRequest(new { Message = "An organization with this slug already exists" });
        }

        var org = new Organization
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            LogoUrl = request.LogoUrl
        };

        _dbContext.Organizations.Add(org);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin created organization: {Name}", org.Name);

        return CreatedAtAction(nameof(GetOrganization), new { id = org.Id }, new OrganizationDto
        {
            Id = org.Id,
            Name = org.Name,
            Slug = org.Slug,
            Description = org.Description,
            LogoUrl = org.LogoUrl,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        });
    }

    [HttpPut("organizations/{id}")]
    public async Task<ActionResult<OrganizationDto>> UpdateOrganization(Guid id, [FromBody] UpdateOrganizationRequest request)
    {
        var org = await _dbContext.Organizations.FindAsync(id);
        if (org == null) return NotFound();

        // Check slug uniqueness if changed
        if (org.Slug.ToLower() != request.Slug.ToLower())
        {
            if (await _dbContext.Organizations.AnyAsync(o => o.Slug.ToLower() == request.Slug.ToLower() && o.Id != id))
            {
                return BadRequest(new { Message = "An organization with this slug already exists" });
            }
        }

        org.Name = request.Name;
        org.Slug = request.Slug;
        org.Description = request.Description;
        org.LogoUrl = request.LogoUrl;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin updated organization: {Name}", org.Name);

        return Ok(new OrganizationDto
        {
            Id = org.Id,
            Name = org.Name,
            Slug = org.Slug,
            Description = org.Description,
            LogoUrl = org.LogoUrl,
            AzureAdTenantId = org.AzureAdTenantId,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        });
    }

    [HttpDelete("organizations/{id}")]
    public async Task<IActionResult> DeleteOrganization(Guid id)
    {
        var org = await _dbContext.Organizations.FindAsync(id);
        if (org == null) return NotFound();

        _dbContext.Organizations.Remove(org);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin deleted organization: {Name}", org.Name);

        return NoContent();
    }

    // ==================== ORGANIZATION MEMBERSHIPS ====================

    [HttpGet("organizations/{orgId}/members")]
    public async Task<ActionResult<List<OrganizationMembershipDto>>> GetOrganizationMembers(Guid orgId)
    {
        var members = await _dbContext.OrganizationMemberships
            .Where(m => m.OrganizationId == orgId)
            .Include(m => m.User)
            .Include(m => m.Organization)
            .Select(m => new OrganizationMembershipDto
            {
                Id = m.Id,
                UserId = m.UserId,
                UserEmail = m.User.Email,
                UserDisplayName = m.User.DisplayName,
                OrganizationId = m.OrganizationId,
                OrganizationName = m.Organization.Name,
                Role = m.Role,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost("memberships")]
    public async Task<ActionResult<OrganizationMembershipDto>> AddMember([FromBody] AddMemberRequest request)
    {
        // Check if already a member
        if (await _dbContext.OrganizationMemberships.AnyAsync(m => m.UserId == request.UserId && m.OrganizationId == request.OrganizationId))
        {
            return BadRequest(new { Message = "User is already a member of this organization" });
        }

        var membership = new OrganizationMembership
        {
            UserId = request.UserId,
            OrganizationId = request.OrganizationId,
            Role = request.Role
        };

        _dbContext.OrganizationMemberships.Add(membership);
        await _dbContext.SaveChangesAsync();

        var result = await _dbContext.OrganizationMemberships
            .Include(m => m.User)
            .Include(m => m.Organization)
            .FirstAsync(m => m.Id == membership.Id);

        return Ok(new OrganizationMembershipDto
        {
            Id = result.Id,
            UserId = result.UserId,
            UserEmail = result.User.Email,
            UserDisplayName = result.User.DisplayName,
            OrganizationId = result.OrganizationId,
            OrganizationName = result.Organization.Name,
            Role = result.Role,
            CreatedAt = result.CreatedAt
        });
    }

    [HttpDelete("memberships/{id}")]
    public async Task<IActionResult> RemoveMember(Guid id)
    {
        var membership = await _dbContext.OrganizationMemberships.FindAsync(id);
        if (membership == null) return NotFound();

        _dbContext.OrganizationMemberships.Remove(membership);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    // ==================== SYSTEM ROLES ====================

    [HttpGet("system-roles")]
    public async Task<ActionResult<List<SystemRoleDto>>> GetSystemRoles([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _dbContext.SystemRoles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(search));
        }

        var roles = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new SystemRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsBuiltIn = r.IsBuiltIn,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                PermissionCount = r.SystemRolePermissions.Count,
                UserCount = _dbContext.UserSystemRoles.Count(ur => ur.SystemRoleId == r.Id)
            })
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("system-roles/count")]
    public async Task<ActionResult<int>> GetSystemRolesCount([FromQuery] string? search)
    {
        var query = _dbContext.SystemRoles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(search));
        }

        return Ok(await query.CountAsync());
    }

    [HttpGet("system-roles/{id}")]
    public async Task<ActionResult<SystemRoleDto>> GetSystemRole(Guid id)
    {
        var role = await _dbContext.SystemRoles
            .Include(r => r.SystemRolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return NotFound();

        return Ok(new SystemRoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsBuiltIn = role.IsBuiltIn,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt,
            PermissionCount = role.SystemRolePermissions.Count,
            UserCount = await _dbContext.UserSystemRoles.CountAsync(ur => ur.SystemRoleId == role.Id)
        });
    }

    [HttpPost("system-roles")]
    public async Task<ActionResult<SystemRoleDto>> CreateSystemRole([FromBody] CreateSystemRoleRequest request)
    {
        var role = new SystemRole
        {
            Name = request.Name,
            Description = request.Description,
            IsBuiltIn = false
        };

        _dbContext.SystemRoles.Add(role);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin created system role: {Name}", role.Name);

        return CreatedAtAction(nameof(GetSystemRole), new { id = role.Id }, new SystemRoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsBuiltIn = role.IsBuiltIn,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        });
    }

    [HttpPut("system-roles/{id}")]
    public async Task<ActionResult<SystemRoleDto>> UpdateSystemRole(Guid id, [FromBody] UpdateSystemRoleRequest request)
    {
        var role = await _dbContext.SystemRoles.FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound();

        if (role.IsBuiltIn)
        {
            return BadRequest(new { Message = "Cannot modify built-in system roles" });
        }

        role.Name = request.Name;
        role.Description = request.Description;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin updated system role: {Name}", role.Name);

        return Ok(new SystemRoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsBuiltIn = role.IsBuiltIn,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        });
    }

    [HttpDelete("system-roles/{id}")]
    public async Task<IActionResult> DeleteSystemRole(Guid id)
    {
        var role = await _dbContext.SystemRoles.FindAsync(id);
        if (role == null) return NotFound();

        if (role.IsBuiltIn)
        {
            return BadRequest(new { Message = "Cannot delete built-in system roles" });
        }

        _dbContext.SystemRoles.Remove(role);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin deleted system role: {Name}", role.Name);

        return NoContent();
    }

    // ==================== PERMISSIONS ====================

    [HttpGet("permissions")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions([FromQuery] string? search, [FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _dbContext.Permissions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        var permissions = await query
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category,
                IsBuiltIn = p.IsBuiltIn,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                SystemRoleCount = p.SystemRolePermissions.Count
            })
            .ToListAsync();

        return Ok(permissions);
    }

    [HttpGet("permissions/count")]
    public async Task<ActionResult<int>> GetPermissionsCount([FromQuery] string? search, [FromQuery] string? category)
    {
        var query = _dbContext.Permissions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        return Ok(await query.CountAsync());
    }

    [HttpGet("permissions/{id}")]
    public async Task<ActionResult<PermissionDto>> GetPermission(Guid id)
    {
        var permission = await _dbContext.Permissions
            .Include(p => p.SystemRolePermissions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (permission == null) return NotFound();

        return Ok(new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            Description = permission.Description,
            Category = permission.Category,
            IsBuiltIn = permission.IsBuiltIn,
            CreatedAt = permission.CreatedAt,
            UpdatedAt = permission.UpdatedAt,
            SystemRoleCount = permission.SystemRolePermissions.Count
        });
    }

    [HttpPost("permissions")]
    public async Task<ActionResult<PermissionDto>> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        var permission = new Permission
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            IsBuiltIn = false
        };

        _dbContext.Permissions.Add(permission);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin created permission: {Name}", permission.Name);

        return CreatedAtAction(nameof(GetPermission), new { id = permission.Id }, new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            Description = permission.Description,
            Category = permission.Category,
            IsBuiltIn = permission.IsBuiltIn,
            CreatedAt = permission.CreatedAt,
            UpdatedAt = permission.UpdatedAt
        });
    }

    [HttpPut("permissions/{id}")]
    public async Task<ActionResult<PermissionDto>> UpdatePermission(Guid id, [FromBody] UpdatePermissionRequest request)
    {
        var permission = await _dbContext.Permissions.FirstOrDefaultAsync(p => p.Id == id);
        if (permission == null) return NotFound();

        if (permission.IsBuiltIn)
        {
            return BadRequest(new { Message = "Cannot modify built-in permissions" });
        }

        permission.Name = request.Name;
        permission.Description = request.Description;
        permission.Category = request.Category;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin updated permission: {Name}", permission.Name);

        return Ok(new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            Description = permission.Description,
            Category = permission.Category,
            IsBuiltIn = permission.IsBuiltIn,
            CreatedAt = permission.CreatedAt,
            UpdatedAt = permission.UpdatedAt
        });
    }

    [HttpDelete("permissions/{id}")]
    public async Task<IActionResult> DeletePermission(Guid id)
    {
        var permission = await _dbContext.Permissions.FindAsync(id);
        if (permission == null) return NotFound();

        if (permission.IsBuiltIn)
        {
            return BadRequest(new { Message = "Cannot delete built-in permissions" });
        }

        _dbContext.Permissions.Remove(permission);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Super admin deleted permission: {Name}", permission.Name);

        return NoContent();
    }

    // Helper methods
    private static string HashPassword(string password)
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);

        using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
            password, salt, 100000, System.Security.Cryptography.HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }
}

public class ResetPasswordRequest
{
    public required string NewPassword { get; set; }
}
