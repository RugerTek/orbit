using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages channels (sales, marketing, distribution, etc.) for the Business Model Canvas.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Channels")]
public class ChannelsController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<ChannelsController> _logger;

    public ChannelsController(OrbitOSDbContext dbContext, ILogger<ChannelsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChannelDto>>> GetChannels(
        Guid organizationId,
        [FromQuery] ChannelType? type = null,
        [FromQuery] ChannelCategory? category = null,
        [FromQuery] ChannelStatus? status = null)
    {
        var query = _dbContext.Channels
            .Include(c => c.Partner)
            .Where(c => c.OrganizationId == organizationId);

        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        if (category.HasValue)
            query = query.Where(c => c.Category == category.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var channels = await query
            .Select(c => new ChannelDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                Type = c.Type,
                Category = c.Category,
                Status = c.Status,
                Ownership = c.Ownership,
                PhasesJson = c.PhasesJson,
                MetricsJson = c.MetricsJson,
                CostJson = c.CostJson,
                IntegrationJson = c.IntegrationJson,
                TagsJson = c.TagsJson,
                OrganizationId = c.OrganizationId,
                PartnerId = c.PartnerId,
                PartnerName = c.Partner != null ? c.Partner.Name : null,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CanvasReferenceCount = c.BlockReferences.Count
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(channels);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChannelDto>> GetChannel(Guid organizationId, Guid id)
    {
        var channel = await _dbContext.Channels
            .Include(c => c.Partner)
            .Where(c => c.Id == id && c.OrganizationId == organizationId)
            .Select(c => new ChannelDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                Type = c.Type,
                Category = c.Category,
                Status = c.Status,
                Ownership = c.Ownership,
                PhasesJson = c.PhasesJson,
                MetricsJson = c.MetricsJson,
                CostJson = c.CostJson,
                IntegrationJson = c.IntegrationJson,
                TagsJson = c.TagsJson,
                OrganizationId = c.OrganizationId,
                PartnerId = c.PartnerId,
                PartnerName = c.Partner != null ? c.Partner.Name : null,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CanvasReferenceCount = c.BlockReferences.Count
            })
            .FirstOrDefaultAsync();

        if (channel == null)
            return NotFound();

        return Ok(channel);
    }

    [HttpPost]
    public async Task<ActionResult<ChannelDto>> CreateChannel(
        Guid organizationId,
        [FromBody] CreateChannelRequest request)
    {
        if (request.PartnerId.HasValue)
        {
            var partnerExists = await _dbContext.Partners
                .AnyAsync(p => p.Id == request.PartnerId.Value && p.OrganizationId == organizationId);
            if (!partnerExists)
                return BadRequest("Invalid partner");
        }

        var channel = new Channel
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Type = request.Type,
            Category = request.Category,
            Status = request.Status,
            Ownership = request.Ownership,
            PhasesJson = request.PhasesJson,
            MetricsJson = request.MetricsJson,
            CostJson = request.CostJson,
            IntegrationJson = request.IntegrationJson,
            TagsJson = request.TagsJson,
            OrganizationId = organizationId,
            PartnerId = request.PartnerId
        };

        _dbContext.Channels.Add(channel);
        await _dbContext.SaveChangesAsync();

        string? partnerName = null;
        if (request.PartnerId.HasValue)
        {
            partnerName = await _dbContext.Partners
                .Where(p => p.Id == request.PartnerId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
        }

        return CreatedAtAction(nameof(GetChannel), new { organizationId, id = channel.Id },
            new ChannelDto
            {
                Id = channel.Id,
                Name = channel.Name,
                Slug = channel.Slug,
                Description = channel.Description,
                Type = channel.Type,
                Category = channel.Category,
                Status = channel.Status,
                Ownership = channel.Ownership,
                PhasesJson = channel.PhasesJson,
                MetricsJson = channel.MetricsJson,
                CostJson = channel.CostJson,
                IntegrationJson = channel.IntegrationJson,
                TagsJson = channel.TagsJson,
                OrganizationId = channel.OrganizationId,
                PartnerId = channel.PartnerId,
                PartnerName = partnerName,
                CreatedAt = channel.CreatedAt,
                UpdatedAt = channel.UpdatedAt,
                CanvasReferenceCount = 0
            });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ChannelDto>> UpdateChannel(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateChannelRequest request)
    {
        var channel = await _dbContext.Channels
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (channel == null)
            return NotFound();

        if (request.PartnerId.HasValue)
        {
            var partnerExists = await _dbContext.Partners
                .AnyAsync(p => p.Id == request.PartnerId.Value && p.OrganizationId == organizationId);
            if (!partnerExists)
                return BadRequest("Invalid partner");
        }

        channel.Name = request.Name;
        channel.Slug = request.Slug;
        channel.Description = request.Description;
        channel.Type = request.Type;
        channel.Category = request.Category;
        channel.Status = request.Status;
        channel.Ownership = request.Ownership;
        channel.PhasesJson = request.PhasesJson;
        channel.MetricsJson = request.MetricsJson;
        channel.CostJson = request.CostJson;
        channel.IntegrationJson = request.IntegrationJson;
        channel.TagsJson = request.TagsJson;
        channel.PartnerId = request.PartnerId;

        await _dbContext.SaveChangesAsync();

        string? partnerName = null;
        if (channel.PartnerId.HasValue)
        {
            partnerName = await _dbContext.Partners
                .Where(p => p.Id == channel.PartnerId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
        }

        var referenceCount = await _dbContext.BlockReferences.CountAsync(r => r.EntityId == id && r.EntityType == ReferenceEntityType.Channel);

        return Ok(new ChannelDto
        {
            Id = channel.Id,
            Name = channel.Name,
            Slug = channel.Slug,
            Description = channel.Description,
            Type = channel.Type,
            Category = channel.Category,
            Status = channel.Status,
            Ownership = channel.Ownership,
            PhasesJson = channel.PhasesJson,
            MetricsJson = channel.MetricsJson,
            CostJson = channel.CostJson,
            IntegrationJson = channel.IntegrationJson,
            TagsJson = channel.TagsJson,
            OrganizationId = channel.OrganizationId,
            PartnerId = channel.PartnerId,
            PartnerName = partnerName,
            CreatedAt = channel.CreatedAt,
            UpdatedAt = channel.UpdatedAt,
            CanvasReferenceCount = referenceCount
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChannel(Guid organizationId, Guid id)
    {
        var channel = await _dbContext.Channels
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (channel == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        channel.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
