using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages partners (suppliers, strategic alliances, distributors, etc.) for the Business Model Canvas.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Partners")]
public class PartnersController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<PartnersController> _logger;

    public PartnersController(OrbitOSDbContext dbContext, ILogger<PartnersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<PartnerDto>>> GetPartners(
        Guid organizationId,
        [FromQuery] PartnerType? type = null,
        [FromQuery] PartnerStatus? status = null)
    {
        var query = _dbContext.Partners
            .Where(p => p.OrganizationId == organizationId);

        if (type.HasValue)
            query = query.Where(p => p.Type == type.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var partners = await query
            .Select(p => new PartnerDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                Type = p.Type,
                Status = p.Status,
                StrategicValue = p.StrategicValue,
                RelationshipStrength = p.RelationshipStrength,
                Website = p.Website,
                ContactJson = p.ContactJson,
                ContractJson = p.ContractJson,
                ServicesProvidedJson = p.ServicesProvidedJson,
                ServicesReceivedJson = p.ServicesReceivedJson,
                CostJson = p.CostJson,
                TagsJson = p.TagsJson,
                OrganizationId = p.OrganizationId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ChannelCount = p.Channels.Count,
                CanvasReferenceCount = p.BlockReferences.Count
            })
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Ok(partners);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PartnerDto>> GetPartner(Guid organizationId, Guid id)
    {
        var partner = await _dbContext.Partners
            .Where(p => p.Id == id && p.OrganizationId == organizationId)
            .Select(p => new PartnerDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                Type = p.Type,
                Status = p.Status,
                StrategicValue = p.StrategicValue,
                RelationshipStrength = p.RelationshipStrength,
                Website = p.Website,
                ContactJson = p.ContactJson,
                ContractJson = p.ContractJson,
                ServicesProvidedJson = p.ServicesProvidedJson,
                ServicesReceivedJson = p.ServicesReceivedJson,
                CostJson = p.CostJson,
                TagsJson = p.TagsJson,
                OrganizationId = p.OrganizationId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ChannelCount = p.Channels.Count,
                CanvasReferenceCount = p.BlockReferences.Count
            })
            .FirstOrDefaultAsync();

        if (partner == null)
            return NotFound();

        return Ok(partner);
    }

    [HttpPost]
    public async Task<ActionResult<PartnerDto>> CreatePartner(
        Guid organizationId,
        [FromBody] CreatePartnerRequest request)
    {
        var partner = new Partner
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Type = request.Type,
            Status = request.Status,
            StrategicValue = request.StrategicValue,
            RelationshipStrength = request.RelationshipStrength,
            Website = request.Website,
            ContactJson = request.ContactJson,
            ContractJson = request.ContractJson,
            ServicesProvidedJson = request.ServicesProvidedJson,
            ServicesReceivedJson = request.ServicesReceivedJson,
            CostJson = request.CostJson,
            TagsJson = request.TagsJson,
            OrganizationId = organizationId
        };

        _dbContext.Partners.Add(partner);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPartner), new { organizationId, id = partner.Id },
            new PartnerDto
            {
                Id = partner.Id,
                Name = partner.Name,
                Slug = partner.Slug,
                Description = partner.Description,
                Type = partner.Type,
                Status = partner.Status,
                StrategicValue = partner.StrategicValue,
                RelationshipStrength = partner.RelationshipStrength,
                Website = partner.Website,
                ContactJson = partner.ContactJson,
                ContractJson = partner.ContractJson,
                ServicesProvidedJson = partner.ServicesProvidedJson,
                ServicesReceivedJson = partner.ServicesReceivedJson,
                CostJson = partner.CostJson,
                TagsJson = partner.TagsJson,
                OrganizationId = partner.OrganizationId,
                CreatedAt = partner.CreatedAt,
                UpdatedAt = partner.UpdatedAt,
                ChannelCount = 0,
                CanvasReferenceCount = 0
            });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PartnerDto>> UpdatePartner(
        Guid organizationId,
        Guid id,
        [FromBody] UpdatePartnerRequest request)
    {
        var partner = await _dbContext.Partners
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        if (partner == null)
            return NotFound();

        partner.Name = request.Name;
        partner.Slug = request.Slug;
        partner.Description = request.Description;
        partner.Type = request.Type;
        partner.Status = request.Status;
        partner.StrategicValue = request.StrategicValue;
        partner.RelationshipStrength = request.RelationshipStrength;
        partner.Website = request.Website;
        partner.ContactJson = request.ContactJson;
        partner.ContractJson = request.ContractJson;
        partner.ServicesProvidedJson = request.ServicesProvidedJson;
        partner.ServicesReceivedJson = request.ServicesReceivedJson;
        partner.CostJson = request.CostJson;
        partner.TagsJson = request.TagsJson;

        await _dbContext.SaveChangesAsync();

        var channelCount = await _dbContext.Channels.CountAsync(c => c.PartnerId == id);
        var referenceCount = await _dbContext.BlockReferences.CountAsync(r => r.EntityId == id && r.EntityType == ReferenceEntityType.Partner);

        return Ok(new PartnerDto
        {
            Id = partner.Id,
            Name = partner.Name,
            Slug = partner.Slug,
            Description = partner.Description,
            Type = partner.Type,
            Status = partner.Status,
            StrategicValue = partner.StrategicValue,
            RelationshipStrength = partner.RelationshipStrength,
            Website = partner.Website,
            ContactJson = partner.ContactJson,
            ContractJson = partner.ContractJson,
            ServicesProvidedJson = partner.ServicesProvidedJson,
            ServicesReceivedJson = partner.ServicesReceivedJson,
            CostJson = partner.CostJson,
            TagsJson = partner.TagsJson,
            OrganizationId = partner.OrganizationId,
            CreatedAt = partner.CreatedAt,
            UpdatedAt = partner.UpdatedAt,
            ChannelCount = channelCount,
            CanvasReferenceCount = referenceCount
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePartner(Guid organizationId, Guid id)
    {
        var partner = await _dbContext.Partners
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        if (partner == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        partner.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
