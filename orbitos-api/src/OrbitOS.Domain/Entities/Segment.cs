using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Represents a customer segment - a group of customers with common needs, behaviors, or attributes.
/// Part of the Business Model Canvas customer segments block.
/// </summary>
public class Segment : BaseEntity
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public SegmentType Type { get; set; } = SegmentType.NicheMarket;
    public SegmentStatus Status { get; set; } = SegmentStatus.Active;

    // Demographics stored as JSON
    public string? DemographicsJson { get; set; }

    // Behaviors stored as JSON
    public string? BehaviorsJson { get; set; }

    // Needs stored as JSON array
    public string? NeedsJson { get; set; }

    // Size and metrics stored as JSON
    public string? MetricsJson { get; set; }

    // Tags stored as JSON array
    public string? TagsJson { get; set; }

    // Multi-tenancy
    public Guid OrganizationId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<ValueProposition> ValuePropositions { get; set; } = new List<ValueProposition>();
    public ICollection<CustomerRelationship> CustomerRelationships { get; set; } = new List<CustomerRelationship>();
    public ICollection<RevenueStream> RevenueStreams { get; set; } = new List<RevenueStream>();
    public ICollection<Canvas> Canvases { get; set; } = new List<Canvas>();
}

public enum SegmentType
{
    MassMarket,
    NicheMarket,
    Segmented,
    Diversified,
    MultiSidedPlatform
}

public enum SegmentStatus
{
    Prospective,
    Active,
    Growing,
    Mature,
    Declining,
    Inactive
}
