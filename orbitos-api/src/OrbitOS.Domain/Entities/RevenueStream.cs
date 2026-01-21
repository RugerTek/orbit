using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// A revenue stream representing how the company generates cash from customer segments.
/// Captures pricing mechanisms, revenue types, and financial projections for each way value is monetized.
/// </summary>
public class RevenueStream : BaseEntity
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required RevenueStreamType Type { get; set; }
    public required RevenueStreamStatus Status { get; set; } = RevenueStreamStatus.Active;
    public required PricingMechanism PricingMechanism { get; set; } = PricingMechanism.Fixed;

    // Pricing details stored as JSON
    public string? PricingJson { get; set; }

    // Revenue figures stored as JSON
    public string? RevenueJson { get; set; }

    // Performance metrics stored as JSON
    public string? MetricsJson { get; set; }

    // Willingness to pay info stored as JSON
    public string? WillingnessToPayJson { get; set; }

    // Tags stored as JSON array
    public string? TagsJson { get; set; }

    // Multi-tenancy
    public Guid OrganizationId { get; set; }

    // Optional relationships
    public Guid? ProductId { get; set; }
    public Guid? SegmentId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Product? Product { get; set; }
    public Segment? Segment { get; set; }
    public ICollection<BlockReference> BlockReferences { get; set; } = new List<BlockReference>();
}

public enum RevenueStreamType
{
    AssetSale,
    UsageFee,
    Subscription,
    Licensing,
    Brokerage,
    Advertising,
    Leasing,
    Commission
}

public enum RevenueStreamStatus
{
    Planned,
    Active,
    Growing,
    Mature,
    Declining,
    Sunset
}

public enum PricingMechanism
{
    Fixed,
    Dynamic,
    Negotiated,
    Auction,
    MarketDependent,
    VolumeDependent
}
