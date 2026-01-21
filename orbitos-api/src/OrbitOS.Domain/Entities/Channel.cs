using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// A channel through which the organization communicates with, reaches, and delivers value to customer segments.
/// Channels cover the customer journey phases: awareness, evaluation, purchase, delivery, and after-sales support.
/// </summary>
public class Channel : BaseEntity
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required ChannelType Type { get; set; }
    public required ChannelCategory Category { get; set; }
    public required ChannelStatus Status { get; set; } = ChannelStatus.Active;
    public required ChannelOwnership Ownership { get; set; } = ChannelOwnership.Owned;

    // Phases stored as JSON array of strings
    public string? PhasesJson { get; set; }

    // Performance metrics stored as JSON
    public string? MetricsJson { get; set; }

    // Cost information stored as JSON
    public string? CostJson { get; set; }

    // Integration info stored as JSON
    public string? IntegrationJson { get; set; }

    // Tags stored as JSON array
    public string? TagsJson { get; set; }

    // Multi-tenancy
    public Guid OrganizationId { get; set; }

    // Optional partner relationship (for partner/third-party owned channels)
    public Guid? PartnerId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Partner? Partner { get; set; }
    public ICollection<BlockReference> BlockReferences { get; set; } = new List<BlockReference>();
}

public enum ChannelType
{
    Direct,
    Indirect,
    Digital,
    Physical,
    Hybrid
}

public enum ChannelCategory
{
    Sales,
    Marketing,
    Distribution,
    Support,
    Communication
}

public enum ChannelStatus
{
    Planned,
    Active,
    Optimizing,
    Sunset,
    Inactive
}

public enum ChannelOwnership
{
    Owned,
    Partner,
    ThirdParty
}
