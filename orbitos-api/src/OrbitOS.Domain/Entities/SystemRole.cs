using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Represents a system-level role that groups permissions for platform access control
/// (e.g., "Super Administrator", "User Administrator").
/// This is distinct from operational Roles which represent job functions in the organization.
/// </summary>
public class SystemRole : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsBuiltIn { get; set; } = true;

    // Navigation properties
    public ICollection<SystemRolePermission> SystemRolePermissions { get; set; } = new List<SystemRolePermission>();
    public ICollection<UserSystemRole> UserSystemRoles { get; set; } = new List<UserSystemRole>();
}
