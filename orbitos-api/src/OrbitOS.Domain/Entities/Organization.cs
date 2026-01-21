using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class Organization : BaseEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? AzureAdTenantId { get; set; }
    public string? Purpose { get; set; } // Organization's WHY

    // Navigation properties
    public ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();
    public ICollection<Function> Functions { get; set; } = new List<Function>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<ResourceSubtype> ResourceSubtypes { get; set; } = new List<ResourceSubtype>();
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    public ICollection<Process> Processes { get; set; } = new List<Process>();
    public ICollection<Canvas> Canvases { get; set; } = new List<Canvas>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public ICollection<AiAgent> AiAgents { get; set; } = new List<AiAgent>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    // Business Model Canvas entities
    public ICollection<Partner> Partners { get; set; } = new List<Partner>();
    public ICollection<Channel> Channels { get; set; } = new List<Channel>();
    public ICollection<ValueProposition> ValuePropositions { get; set; } = new List<ValueProposition>();
    public ICollection<CustomerRelationship> CustomerRelationships { get; set; } = new List<CustomerRelationship>();
    public ICollection<RevenueStream> RevenueStreams { get; set; } = new List<RevenueStream>();
    public ICollection<CanvasBlock> CanvasBlocks { get; set; } = new List<CanvasBlock>();
    public ICollection<BlockReference> BlockReferences { get; set; } = new List<BlockReference>();
}
