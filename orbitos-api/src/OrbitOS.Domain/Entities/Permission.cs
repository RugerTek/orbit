using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Represents a system-level permission that controls what actions users can perform
/// in the platform (e.g., "users.view", "org.settings.manage").
/// This is distinct from operational Functions which represent business work units.
/// </summary>
public class Permission : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
    public bool IsBuiltIn { get; set; } = true;

    // Navigation properties
    public ICollection<SystemRolePermission> SystemRolePermissions { get; set; } = new List<SystemRolePermission>();
}
