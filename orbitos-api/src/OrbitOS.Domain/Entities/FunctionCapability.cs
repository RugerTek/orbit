using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Records that a person Resource CAN perform a Function
/// Enables tracking of who can do what, independent of role assignments
/// </summary>
public class FunctionCapability : BaseEntity
{
    public Guid ResourceId { get; set; }
    public Guid FunctionId { get; set; }
    public required CapabilityLevel Level { get; set; } = CapabilityLevel.Capable;
    public DateTime? CertifiedDate { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Resource Resource { get; set; } = null!;
    public Function Function { get; set; } = null!;
}

public enum CapabilityLevel
{
    Learning,
    Capable,
    Proficient,
    Expert,
    Trainer
}
