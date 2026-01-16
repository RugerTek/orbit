using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class CanvasBlock : BaseEntity
{
    public required CanvasBlockType BlockType { get; set; }
    public string? Content { get; set; } // JSON array of items
    public int DisplayOrder { get; set; }

    public Guid CanvasId { get; set; }

    // Navigation properties
    public Canvas Canvas { get; set; } = null!;
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
