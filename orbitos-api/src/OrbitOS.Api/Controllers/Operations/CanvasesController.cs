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

    [HttpGet]
    public async Task<ActionResult<List<CanvasDto>>> GetCanvases(Guid organizationId)
    {
        var canvases = await _dbContext.Canvases
            .Where(c => c.OrganizationId == organizationId)
            .Select(c => new CanvasDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CanvasType = c.CanvasType,
                Status = c.Status,
                OrganizationId = c.OrganizationId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(canvases);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CanvasDto>> GetCanvas(Guid organizationId, Guid id)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Blocks)
            .Where(c => c.Id == id && c.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (canvas == null)
            return NotFound();

        return Ok(new CanvasDto
        {
            Id = canvas.Id,
            Name = canvas.Name,
            Description = canvas.Description,
            CanvasType = canvas.CanvasType,
            Status = canvas.Status,
            OrganizationId = canvas.OrganizationId,
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
        });
    }

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
            Status = request.Status,
            OrganizationId = organizationId
        };

        // Create default blocks for Business Model Canvas
        var blockTypes = Enum.GetValues<CanvasBlockType>();
        foreach (var blockType in blockTypes)
        {
            canvas.Blocks.Add(new CanvasBlock
            {
                BlockType = blockType,
                Content = "[]",
                DisplayOrder = (int)blockType
            });
        }

        _dbContext.Canvases.Add(canvas);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCanvas), new { organizationId, id = canvas.Id },
            new CanvasDto
            {
                Id = canvas.Id,
                Name = canvas.Name,
                Description = canvas.Description,
                CanvasType = canvas.CanvasType,
                Status = canvas.Status,
                OrganizationId = canvas.OrganizationId,
                CreatedAt = canvas.CreatedAt,
                UpdatedAt = canvas.UpdatedAt,
                Blocks = canvas.Blocks.Select(b => new CanvasBlockDto
                {
                    Id = b.Id,
                    BlockType = b.BlockType,
                    Content = b.Content,
                    DisplayOrder = b.DisplayOrder,
                    CanvasId = b.CanvasId
                }).ToList()
            });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CanvasDto>> UpdateCanvas(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateCanvasRequest request)
    {
        var canvas = await _dbContext.Canvases
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (canvas == null)
            return NotFound();

        canvas.Name = request.Name;
        canvas.Description = request.Description;
        canvas.CanvasType = request.CanvasType;
        canvas.Status = request.Status;

        await _dbContext.SaveChangesAsync();

        return Ok(new CanvasDto
        {
            Id = canvas.Id,
            Name = canvas.Name,
            Description = canvas.Description,
            CanvasType = canvas.CanvasType,
            Status = canvas.Status,
            OrganizationId = canvas.OrganizationId,
            CreatedAt = canvas.CreatedAt,
            UpdatedAt = canvas.UpdatedAt
        });
    }

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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCanvas(Guid organizationId, Guid id)
    {
        var canvas = await _dbContext.Canvases
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (canvas == null)
            return NotFound();

        _dbContext.Canvases.Remove(canvas);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
