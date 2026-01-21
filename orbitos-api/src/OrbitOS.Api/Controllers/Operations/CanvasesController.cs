using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages business model canvases and their blocks.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Canvases")]
public class CanvasesController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<CanvasesController> _logger;

    public CanvasesController(OrbitOSDbContext dbContext, ILogger<CanvasesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all canvases for an organization
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CanvasDto>>> GetCanvases(
        Guid organizationId,
        [FromQuery] CanvasScopeType? scopeType = null,
        [FromQuery] CanvasStatus? status = null,
        [FromQuery] Guid? productId = null)
    {
        var query = _dbContext.Canvases
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .Where(c => c.OrganizationId == organizationId);

        if (scopeType.HasValue)
            query = query.Where(c => c.ScopeType == scopeType.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (productId.HasValue)
            query = query.Where(c => c.ProductId == productId.Value);

        var canvases = await query
            .Select(c => new CanvasDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                CanvasType = c.CanvasType,
                ScopeType = c.ScopeType,
                Status = c.Status,
                Version = c.Version,
                VersionNote = c.VersionNote,
                AiSummary = c.AiSummary,
                OrganizationId = c.OrganizationId,
                ParentCanvasId = c.ParentCanvasId,
                ProductId = c.ProductId,
                ProductName = c.Product != null ? c.Product.Name : null,
                SegmentId = c.SegmentId,
                SegmentName = c.Segment != null ? c.Segment.Name : null,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .OrderBy(c => c.ScopeType)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return Ok(canvases);
    }

    /// <summary>
    /// Get BMC canvases (Business Model Canvas type only) - convenience endpoint
    /// </summary>
    [HttpGet("bmc")]
    public async Task<ActionResult<List<CanvasDto>>> GetBmcCanvases(
        Guid organizationId,
        [FromQuery] CanvasScopeType? scopeType = null,
        [FromQuery] CanvasStatus? status = null,
        [FromQuery] Guid? productId = null)
    {
        var query = _dbContext.Canvases
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .Where(c => c.OrganizationId == organizationId && c.CanvasType == CanvasType.BusinessModel);

        if (scopeType.HasValue)
            query = query.Where(c => c.ScopeType == scopeType.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (productId.HasValue)
            query = query.Where(c => c.ProductId == productId.Value);

        var canvases = await query
            .Select(c => new CanvasDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                CanvasType = c.CanvasType,
                ScopeType = c.ScopeType,
                Status = c.Status,
                Version = c.Version,
                VersionNote = c.VersionNote,
                AiSummary = c.AiSummary,
                OrganizationId = c.OrganizationId,
                ParentCanvasId = c.ParentCanvasId,
                ProductId = c.ProductId,
                ProductName = c.Product != null ? c.Product.Name : null,
                SegmentId = c.SegmentId,
                SegmentName = c.Segment != null ? c.Segment.Name : null,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .OrderBy(c => c.ScopeType)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return Ok(canvases);
    }

    /// <summary>
    /// Get a specific canvas by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CanvasDto>> GetCanvas(Guid organizationId, Guid id)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Blocks)
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .Where(c => c.Id == id && c.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (canvas == null)
            return NotFound();

        return Ok(MapToDto(canvas));
    }

    /// <summary>
    /// Get a specific BMC canvas by ID
    /// </summary>
    [HttpGet("bmc/{id}")]
    public async Task<ActionResult<CanvasDto>> GetBmcCanvas(Guid organizationId, Guid id)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Blocks)
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .Where(c => c.Id == id && c.OrganizationId == organizationId && c.CanvasType == CanvasType.BusinessModel)
            .FirstOrDefaultAsync();

        if (canvas == null)
            return NotFound();

        return Ok(MapToDto(canvas));
    }

    /// <summary>
    /// Create a new canvas
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CanvasDto>> CreateCanvas(
        Guid organizationId,
        [FromBody] CreateCanvasRequest request)
    {
        var canvas = new Canvas
        {
            Name = request.Name,
            Description = request.Description,
            CanvasType = request.CanvasType,
            ScopeType = request.ScopeType,
            Status = request.Status,
            OrganizationId = organizationId,
            ParentCanvasId = request.ParentCanvasId,
            ProductId = request.ProductId,
            SegmentId = request.SegmentId,
            Version = 1
        };

        // Create default blocks for Business Model Canvas
        if (request.CanvasType == CanvasType.BusinessModel)
        {
            var blockTypes = Enum.GetValues<CanvasBlockType>();
            foreach (var blockType in blockTypes)
            {
                canvas.Blocks.Add(new CanvasBlock
                {
                    BlockType = blockType,
                    Content = "[]",
                    DisplayOrder = (int)blockType,
                    OrganizationId = organizationId
                });
            }
        }

        _dbContext.Canvases.Add(canvas);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCanvas), new { organizationId, id = canvas.Id }, MapToDto(canvas));
    }

    /// <summary>
    /// Create a new BMC canvas (Business Model Canvas)
    /// </summary>
    [HttpPost("bmc")]
    public async Task<ActionResult<CanvasDto>> CreateBmcCanvas(
        Guid organizationId,
        [FromBody] CreateCanvasRequest request)
    {
        // Force BusinessModel type
        request.CanvasType = CanvasType.BusinessModel;

        var canvas = new Canvas
        {
            Name = request.Name,
            Description = request.Description,
            CanvasType = CanvasType.BusinessModel,
            ScopeType = request.ScopeType,
            Status = request.Status,
            OrganizationId = organizationId,
            ParentCanvasId = request.ParentCanvasId,
            ProductId = request.ProductId,
            SegmentId = request.SegmentId,
            Version = 1
        };

        // Create default blocks for Business Model Canvas
        var blockTypes = Enum.GetValues<CanvasBlockType>();
        foreach (var blockType in blockTypes)
        {
            canvas.Blocks.Add(new CanvasBlock
            {
                BlockType = blockType,
                Content = "[]",
                DisplayOrder = (int)blockType,
                OrganizationId = organizationId
            });
        }

        _dbContext.Canvases.Add(canvas);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBmcCanvas), new { organizationId, id = canvas.Id }, MapToDto(canvas));
    }

    /// <summary>
    /// Update an existing canvas
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CanvasDto>> UpdateCanvas(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateCanvasRequest request)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (canvas == null)
            return NotFound();

        canvas.Name = request.Name;
        canvas.Description = request.Description;
        canvas.CanvasType = request.CanvasType;
        canvas.ScopeType = request.ScopeType;
        canvas.Status = request.Status;
        canvas.ParentCanvasId = request.ParentCanvasId;
        canvas.ProductId = request.ProductId;
        canvas.SegmentId = request.SegmentId;

        await _dbContext.SaveChangesAsync();

        return Ok(MapToDto(canvas));
    }

    /// <summary>
    /// Update an existing BMC canvas
    /// </summary>
    [HttpPut("bmc/{id}")]
    public async Task<ActionResult<CanvasDto>> UpdateBmcCanvas(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateCanvasRequest request)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.CanvasType == CanvasType.BusinessModel);

        if (canvas == null)
            return NotFound();

        canvas.Name = request.Name;
        canvas.Description = request.Description;
        canvas.ScopeType = request.ScopeType;
        canvas.Status = request.Status;
        canvas.ParentCanvasId = request.ParentCanvasId;
        canvas.ProductId = request.ProductId;
        canvas.SegmentId = request.SegmentId;

        await _dbContext.SaveChangesAsync();

        return Ok(MapToDto(canvas));
    }

    /// <summary>
    /// Update a specific block within a canvas
    /// </summary>
    [HttpPut("{canvasId}/blocks/{blockType}")]
    public async Task<ActionResult<CanvasBlockDto>> UpdateCanvasBlock(
        Guid organizationId,
        Guid canvasId,
        CanvasBlockType blockType,
        [FromBody] UpdateCanvasBlockRequest request)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Blocks)
            .FirstOrDefaultAsync(c => c.Id == canvasId && c.OrganizationId == organizationId);

        if (canvas == null)
            return NotFound("Canvas not found");

        var block = canvas.Blocks.FirstOrDefault(b => b.BlockType == blockType);
        if (block == null)
        {
            block = new CanvasBlock
            {
                BlockType = blockType,
                Content = request.Content,
                DisplayOrder = request.DisplayOrder,
                CanvasId = canvasId
            };
            _dbContext.CanvasBlocks.Add(block);
        }
        else
        {
            block.Content = request.Content;
            block.DisplayOrder = request.DisplayOrder;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new CanvasBlockDto
        {
            Id = block.Id,
            BlockType = block.BlockType,
            Content = block.Content,
            DisplayOrder = block.DisplayOrder,
            CanvasId = block.CanvasId
        });
    }

    /// <summary>
    /// Delete a canvas
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCanvas(Guid organizationId, Guid id)
    {
        var canvas = await _dbContext.Canvases
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (canvas == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        canvas.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Delete a BMC canvas
    /// </summary>
    [HttpDelete("bmc/{id}")]
    public async Task<IActionResult> DeleteBmcCanvas(Guid organizationId, Guid id)
    {
        var canvas = await _dbContext.Canvases
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.CanvasType == CanvasType.BusinessModel);

        if (canvas == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        canvas.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static CanvasDto MapToDto(Canvas canvas)
    {
        return new CanvasDto
        {
            Id = canvas.Id,
            Name = canvas.Name,
            Slug = canvas.Slug,
            Description = canvas.Description,
            CanvasType = canvas.CanvasType,
            ScopeType = canvas.ScopeType,
            Status = canvas.Status,
            Version = canvas.Version,
            VersionNote = canvas.VersionNote,
            AiSummary = canvas.AiSummary,
            OrganizationId = canvas.OrganizationId,
            ParentCanvasId = canvas.ParentCanvasId,
            ProductId = canvas.ProductId,
            ProductName = canvas.Product?.Name,
            SegmentId = canvas.SegmentId,
            SegmentName = canvas.Segment?.Name,
            CreatedAt = canvas.CreatedAt,
            UpdatedAt = canvas.UpdatedAt,
            Blocks = canvas.Blocks.OrderBy(b => b.DisplayOrder).Select(b => new CanvasBlockDto
            {
                Id = b.Id,
                BlockType = b.BlockType,
                Content = b.Content,
                DisplayOrder = b.DisplayOrder,
                CanvasId = b.CanvasId
            }).ToList()
        };
    }
}
