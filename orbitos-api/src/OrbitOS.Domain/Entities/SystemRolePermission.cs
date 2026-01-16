using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Junction table linking SystemRoles to Permissions.
/// Defines which permissions are granted to each system role.
/// </summary>
public class SystemRolePermission : BaseEntity
{
    public Guid SystemRoleId { get; set; }
    public Guid PermissionId { get; set; }

    // Navigation properties
    public SystemRole SystemRole { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
