using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// An external organization that contributes to the business model.
/// Partners include suppliers, strategic alliances, technology partners, distributors, and other key relationships.
/// </summary>
public class Partner : BaseEntity
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required PartnerType Type { get; set; }
    public required PartnerStatus Status { get; set; } = PartnerStatus.Active;
    public required StrategicValue StrategicValue { get; set; } = StrategicValue.Medium;
    public int? RelationshipStrength { get; set; } // 1-5 rating
    public string? Website { get; set; }

    // Contact information stored as JSON
    public string? ContactJson { get; set; }

    // Contract details stored as JSON
    public string? ContractJson { get; set; }

    // Services provided/received stored as JSON arrays
    public string? ServicesProvidedJson { get; set; }
    public string? ServicesReceivedJson { get; set; }

    // Cost information stored as JSON
    public string? CostJson { get; set; }

    // Tags stored as JSON array
    public string? TagsJson { get; set; }

    // Multi-tenancy
    public Guid OrganizationId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<Channel> Channels { get; set; } = new List<Channel>();
    public ICollection<BlockReference> BlockReferences { get; set; } = new List<BlockReference>();
}

public enum PartnerType
{
    Supplier,
    Distributor,
    Strategic,
    Technology,
    Agency,
    Reseller,
    Affiliate,
    JointVenture
}

public enum PartnerStatus
{
    Prospective,
    Active,
    OnHold,
    Terminated
}

public enum StrategicValue
{
    Critical,
    High,
    Medium,
    Low
}
