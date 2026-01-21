using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// A block within a Business Model Canvas representing one of the 9 standard BMC sections.
/// Blocks aggregate references to operational entities and provide canvas-specific context.
/// </summary>
public class CanvasBlock : BaseEntity
{
    public required CanvasBlockType BlockType { get; set; }
    public string? Title { get; set; } // Custom title override
    public string? SummaryNote { get; set; }
    public int SortOrder { get; set; }
    public int DisplayOrder { get; set; }

    // Content stored as JSON array of strings
    public string? Content { get; set; }

    // Position stored as JSON
    public string? PositionJson { get; set; }

    // AI insights stored as JSON
    public string? AiInsightsJson { get; set; }

    // Multi-tenancy (denormalized for query efficiency)
    public Guid OrganizationId { get; set; }

    public Guid CanvasId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Canvas Canvas { get; set; } = null!;
    public ICollection<BlockReference> References { get; set; } = new List<BlockReference>();
}

public enum CanvasBlockType
{
    KeyPartners,
    KeyActivities,
    KeyResources,
    ValuePropositions,
    CustomerRelationships,
    Channels,
    CustomerSegments,
    CostStructure,
    RevenueStreams
}
