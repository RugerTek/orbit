using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// A value proposition that describes how a product or service creates value for a customer segment.
/// Based on the Value Proposition Canvas methodology - captures customer jobs, pains, gains and how the offering addresses them.
/// </summary>
public class ValueProposition : BaseEntity
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public required string Headline { get; set; }
    public string? Description { get; set; }
    public required ValuePropositionStatus Status { get; set; } = ValuePropositionStatus.Draft;

    // Customer jobs stored as JSON array
    public string? CustomerJobsJson { get; set; }

    // Pains stored as JSON array
    public string? PainsJson { get; set; }

    // Gains stored as JSON array
    public string? GainsJson { get; set; }

    // Pain relievers stored as JSON array
    public string? PainRelieversJson { get; set; }

    // Gain creators stored as JSON array
    public string? GainCreatorsJson { get; set; }

    // Products/services stored as JSON array
    public string? ProductsServicesJson { get; set; }

    // Differentiators stored as JSON array
    public string? DifferentiatorsJson { get; set; }

    // Validation evidence stored as JSON
    public string? ValidationJson { get; set; }

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

public enum ValuePropositionStatus
{
    Draft,
    Validated,
    Active,
    Testing,
    Retired
}
