using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages operational processes and their activities.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Processes")]
public class ProcessesController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<ProcessesController> _logger;

    public ProcessesController(OrbitOSDbContext dbContext, ILogger<ProcessesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    #region Processes

    [HttpGet]
    public async Task<ActionResult<List<ProcessDto>>> GetProcesses(Guid organizationId, [FromQuery] ProcessStateType? stateType = null)
    {
        var query = _dbContext.Processes
            .Include(p => p.Owner)
            .Include(p => p.Activities)
            .Where(p => p.OrganizationId == organizationId);

        if (stateType.HasValue)
        {
            query = query.Where(p => p.StateType == stateType.Value);
        }

        var processes = await query
            .Select(p => new ProcessDto
            {
                Id = p.Id,
                Name = p.Name,
                Purpose = p.Purpose,
                Description = p.Description,
                Trigger = p.Trigger,
                Output = p.Output,
                Frequency = p.Frequency,
                Status = p.Status,
                StateType = p.StateType,
                OrganizationId = p.OrganizationId,
                OwnerId = p.OwnerId,
                OwnerName = p.Owner != null ? p.Owner.Name : null,
                LinkedProcessId = p.LinkedProcessId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ActivityCount = p.Activities.Count
            })
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Ok(processes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProcessDto>> GetProcess(Guid organizationId, Guid id)
    {
        var process = await _dbContext.Processes
            .Include(p => p.Owner)
            .Include(p => p.LinkedProcess)
            .Include(p => p.Activities)
                .ThenInclude(a => a.Function)
            .Include(p => p.Activities)
                .ThenInclude(a => a.AssignedResource)
            .Include(p => p.Activities)
                .ThenInclude(a => a.LinkedProcess)
                    .ThenInclude(lp => lp!.Activities)
            .Include(p => p.Edges)
            .Where(p => p.Id == id && p.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (process == null)
            return NotFound();

        return Ok(new ProcessDto
        {
            Id = process.Id,
            Name = process.Name,
            Purpose = process.Purpose,
            Description = process.Description,
            Trigger = process.Trigger,
            Output = process.Output,
            Frequency = process.Frequency,
            Status = process.Status,
            StateType = process.StateType,
            OrganizationId = process.OrganizationId,
            OwnerId = process.OwnerId,
            OwnerName = process.Owner?.Name,
            LinkedProcessId = process.LinkedProcessId,
            LinkedProcessName = process.LinkedProcess?.Name,
            EntryActivityId = process.EntryActivityId,
            ExitActivityId = process.ExitActivityId,
            UseExplicitFlow = process.UseExplicitFlow,
            CreatedAt = process.CreatedAt,
            UpdatedAt = process.UpdatedAt,
            ActivityCount = process.Activities.Count,
            Activities = process.Activities.OrderBy(a => a.Order).Select(a => new ActivityDto
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Order = a.Order,
                ActivityType = a.ActivityType,
                EstimatedDurationMinutes = a.EstimatedDurationMinutes,
                Instructions = a.Instructions,
                ProcessId = a.ProcessId,
                FunctionId = a.FunctionId,
                FunctionName = a.Function?.Name,
                AssignedResourceId = a.AssignedResourceId,
                AssignedResourceName = a.AssignedResource?.Name,
                LinkedProcessId = a.LinkedProcessId,
                LinkedProcessName = a.LinkedProcess?.Name,
                // Full subprocess summary for "portal" display
                LinkedProcess = a.LinkedProcess != null ? new LinkedProcessSummaryDto
                {
                    Id = a.LinkedProcess.Id,
                    Name = a.LinkedProcess.Name,
                    Purpose = a.LinkedProcess.Purpose,
                    Trigger = a.LinkedProcess.Trigger,
                    Output = a.LinkedProcess.Output,
                    ActivityCount = a.LinkedProcess.Activities.Count
                } : null,
                CreatedAt = a.CreatedAt,
                PositionX = a.PositionX,
                PositionY = a.PositionY,
                MetadataJson = a.MetadataJson
            }).ToList(),
            Edges = process.Edges.Select(e => new ActivityEdgeDto
            {
                Id = e.Id,
                ProcessId = e.ProcessId,
                SourceActivityId = e.SourceActivityId,
                TargetActivityId = e.TargetActivityId,
                SourceHandle = e.SourceHandle,
                TargetHandle = e.TargetHandle,
                EdgeType = e.EdgeType,
                Label = e.Label,
                Animated = e.Animated,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<ProcessDto>> CreateProcess(
        Guid organizationId,
        [FromBody] CreateProcessRequest request)
    {
        var process = new Process
        {
            Name = request.Name,
            Purpose = request.Purpose,
            Description = request.Description,
            Trigger = request.Trigger,
            Output = request.Output,
            Frequency = request.Frequency,
            Status = request.Status,
            StateType = request.StateType,
            OrganizationId = organizationId,
            OwnerId = request.OwnerId,
            LinkedProcessId = request.LinkedProcessId
        };

        _dbContext.Processes.Add(process);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProcess), new { organizationId, id = process.Id },
            new ProcessDto
            {
                Id = process.Id,
                Name = process.Name,
                Purpose = process.Purpose,
                Description = process.Description,
                Trigger = process.Trigger,
                Output = process.Output,
                Frequency = process.Frequency,
                Status = process.Status,
                StateType = process.StateType,
                OrganizationId = process.OrganizationId,
                OwnerId = process.OwnerId,
                LinkedProcessId = process.LinkedProcessId,
                CreatedAt = process.CreatedAt,
                UpdatedAt = process.UpdatedAt,
                ActivityCount = 0
            });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProcessDto>> UpdateProcess(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateProcessRequest request)
    {
        var process = await _dbContext.Processes
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        if (process == null)
            return NotFound();

        process.Name = request.Name;
        process.Purpose = request.Purpose;
        process.Description = request.Description;
        process.Trigger = request.Trigger;
        process.Output = request.Output;
        process.Frequency = request.Frequency;
        process.Status = request.Status;
        process.StateType = request.StateType;
        process.OwnerId = request.OwnerId;
        process.LinkedProcessId = request.LinkedProcessId;
        process.EntryActivityId = request.EntryActivityId;
        process.ExitActivityId = request.ExitActivityId;

        await _dbContext.SaveChangesAsync();

        return Ok(new ProcessDto
        {
            Id = process.Id,
            Name = process.Name,
            Purpose = process.Purpose,
            Description = process.Description,
            Trigger = process.Trigger,
            Output = process.Output,
            Frequency = process.Frequency,
            Status = process.Status,
            StateType = process.StateType,
            OrganizationId = process.OrganizationId,
            OwnerId = process.OwnerId,
            OwnerName = process.Owner?.Name,
            LinkedProcessId = process.LinkedProcessId,
            EntryActivityId = process.EntryActivityId,
            ExitActivityId = process.ExitActivityId,
            CreatedAt = process.CreatedAt,
            UpdatedAt = process.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProcess(Guid organizationId, Guid id)
    {
        var process = await _dbContext.Processes
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        if (process == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        process.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Update the entry and/or exit activity for a process flow.
    /// Used when connecting from Start node or to End node in the flow editor.
    /// </summary>
    [HttpPatch("{id}/flow-endpoints")]
    public async Task<IActionResult> UpdateFlowEndpoints(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateFlowEndpointsRequest request)
    {
        var process = await _dbContext.Processes
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        if (process == null)
            return NotFound();

        // Validate that the referenced activities exist and belong to this process
        if (request.EntryActivityId.HasValue)
        {
            var activityExists = await _dbContext.Activities
                .AnyAsync(a => a.Id == request.EntryActivityId.Value && a.ProcessId == id);
            if (!activityExists)
                return BadRequest("Entry activity not found in this process");
            process.EntryActivityId = request.EntryActivityId;
        }

        if (request.ExitActivityId.HasValue)
        {
            var activityExists = await _dbContext.Activities
                .AnyAsync(a => a.Id == request.ExitActivityId.Value && a.ProcessId == id);
            if (!activityExists)
                return BadRequest("Exit activity not found in this process");
            process.ExitActivityId = request.ExitActivityId;
        }

        // Allow clearing the fields by explicitly passing null
        if (request.ClearEntry)
            process.EntryActivityId = null;
        if (request.ClearExit)
            process.ExitActivityId = null;

        // Mark as explicit flow mode when user sets flow endpoints
        process.UseExplicitFlow = true;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            EntryActivityId = process.EntryActivityId,
            ExitActivityId = process.ExitActivityId,
            UseExplicitFlow = process.UseExplicitFlow
        });
    }

    #endregion

    #region Activities

    [HttpPost("{processId}/activities")]
    public async Task<ActionResult<ActivityDto>> CreateActivity(
        Guid organizationId,
        Guid processId,
        [FromBody] CreateActivityRequest request)
    {
        var process = await _dbContext.Processes
            .FirstOrDefaultAsync(p => p.Id == processId && p.OrganizationId == organizationId);

        if (process == null)
            return NotFound("Process not found");

        var activity = new Activity
        {
            Name = request.Name,
            Description = request.Description,
            Order = request.Order,
            ActivityType = request.ActivityType,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            Instructions = request.Instructions,
            ProcessId = processId,
            FunctionId = request.FunctionId,
            AssignedResourceId = request.AssignedResourceId,
            LinkedProcessId = request.LinkedProcessId,
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            MetadataJson = request.MetadataJson
        };

        _dbContext.Activities.Add(activity);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProcess), new { organizationId, id = processId },
            new ActivityDto
            {
                Id = activity.Id,
                Name = activity.Name,
                Description = activity.Description,
                Order = activity.Order,
                ActivityType = activity.ActivityType,
                EstimatedDurationMinutes = activity.EstimatedDurationMinutes,
                Instructions = activity.Instructions,
                ProcessId = activity.ProcessId,
                FunctionId = activity.FunctionId,
                AssignedResourceId = activity.AssignedResourceId,
                LinkedProcessId = activity.LinkedProcessId,
                CreatedAt = activity.CreatedAt,
                PositionX = activity.PositionX,
                PositionY = activity.PositionY,
                MetadataJson = activity.MetadataJson
            });
    }

    [HttpPut("{processId}/activities/{id}")]
    public async Task<ActionResult<ActivityDto>> UpdateActivity(
        Guid organizationId,
        Guid processId,
        Guid id,
        [FromBody] UpdateActivityRequest request)
    {
        var activity = await _dbContext.Activities
            .Include(a => a.Process)
            .Include(a => a.Function)
            .Include(a => a.AssignedResource)
            .Include(a => a.LinkedProcess)
            .FirstOrDefaultAsync(a => a.Id == id && a.ProcessId == processId && a.Process.OrganizationId == organizationId);

        if (activity == null)
            return NotFound();

        activity.Name = request.Name;
        activity.Description = request.Description;
        activity.Order = request.Order;
        activity.ActivityType = request.ActivityType;
        activity.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        activity.Instructions = request.Instructions;
        activity.FunctionId = request.FunctionId;
        activity.AssignedResourceId = request.AssignedResourceId;
        activity.LinkedProcessId = request.LinkedProcessId;
        activity.PositionX = request.PositionX;
        activity.PositionY = request.PositionY;
        activity.MetadataJson = request.MetadataJson;

        await _dbContext.SaveChangesAsync();

        // Reload the linked process if it was set
        if (activity.LinkedProcessId.HasValue && activity.LinkedProcess == null)
        {
            activity.LinkedProcess = await _dbContext.Processes.FindAsync(activity.LinkedProcessId);
        }

        return Ok(new ActivityDto
        {
            Id = activity.Id,
            Name = activity.Name,
            Description = activity.Description,
            Order = activity.Order,
            ActivityType = activity.ActivityType,
            EstimatedDurationMinutes = activity.EstimatedDurationMinutes,
            Instructions = activity.Instructions,
            ProcessId = activity.ProcessId,
            FunctionId = activity.FunctionId,
            FunctionName = activity.Function?.Name,
            AssignedResourceId = activity.AssignedResourceId,
            AssignedResourceName = activity.AssignedResource?.Name,
            LinkedProcessId = activity.LinkedProcessId,
            LinkedProcessName = activity.LinkedProcess?.Name,
            CreatedAt = activity.CreatedAt,
            PositionX = activity.PositionX,
            PositionY = activity.PositionY,
            MetadataJson = activity.MetadataJson
        });
    }

    [HttpDelete("{processId}/activities/{id}")]
    public async Task<IActionResult> DeleteActivity(Guid organizationId, Guid processId, Guid id)
    {
        var activity = await _dbContext.Activities
            .Include(a => a.Process)
            .FirstOrDefaultAsync(a => a.Id == id && a.ProcessId == processId && a.Process.OrganizationId == organizationId);

        if (activity == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        activity.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{processId}/activities/reorder")]
    public async Task<IActionResult> ReorderActivities(
        Guid organizationId,
        Guid processId,
        [FromBody] ReorderActivitiesRequest request)
    {
        var process = await _dbContext.Processes
            .Include(p => p.Activities)
            .FirstOrDefaultAsync(p => p.Id == processId && p.OrganizationId == organizationId);

        if (process == null)
            return NotFound("Process not found");

        for (int i = 0; i < request.ActivityIds.Count; i++)
        {
            var activity = process.Activities.FirstOrDefault(a => a.Id == request.ActivityIds[i]);
            if (activity != null)
            {
                activity.Order = i;
            }
        }

        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Bulk update activity positions (Vue Flow canvas drag).
    /// </summary>
    [HttpPatch("{processId}/activities/positions")]
    public async Task<IActionResult> UpdateActivityPositions(
        Guid organizationId,
        Guid processId,
        [FromBody] UpdateActivityPositionsRequest request)
    {
        var process = await _dbContext.Processes
            .Include(p => p.Activities)
            .FirstOrDefaultAsync(p => p.Id == processId && p.OrganizationId == organizationId);

        if (process == null)
            return NotFound("Process not found");

        foreach (var pos in request.Positions)
        {
            var activity = process.Activities.FirstOrDefault(a => a.Id == pos.ActivityId);
            if (activity != null)
            {
                activity.PositionX = pos.PositionX;
                activity.PositionY = pos.PositionY;
            }
        }

        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    #endregion

    #region Activity Edges (Vue Flow connections)

    /// <summary>
    /// Get all edges for a process.
    /// </summary>
    [HttpGet("{processId}/edges")]
    public async Task<ActionResult<List<ActivityEdgeDto>>> GetEdges(Guid organizationId, Guid processId)
    {
        var process = await _dbContext.Processes
            .FirstOrDefaultAsync(p => p.Id == processId && p.OrganizationId == organizationId);

        if (process == null)
            return NotFound("Process not found");

        var edges = await _dbContext.ActivityEdges
            .Where(e => e.ProcessId == processId)
            .Select(e => new ActivityEdgeDto
            {
                Id = e.Id,
                ProcessId = e.ProcessId,
                SourceActivityId = e.SourceActivityId,
                TargetActivityId = e.TargetActivityId,
                SourceHandle = e.SourceHandle,
                TargetHandle = e.TargetHandle,
                EdgeType = e.EdgeType,
                Label = e.Label,
                Animated = e.Animated,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .ToListAsync();

        return Ok(edges);
    }

    /// <summary>
    /// Create a new edge between activities.
    /// </summary>
    [HttpPost("{processId}/edges")]
    public async Task<ActionResult<ActivityEdgeDto>> CreateEdge(
        Guid organizationId,
        Guid processId,
        [FromBody] CreateActivityEdgeRequest request)
    {
        var process = await _dbContext.Processes
            .FirstOrDefaultAsync(p => p.Id == processId && p.OrganizationId == organizationId);

        if (process == null)
            return NotFound("Process not found");

        // Validate source and target activities exist and belong to this process
        var sourceExists = await _dbContext.Activities
            .AnyAsync(a => a.Id == request.SourceActivityId && a.ProcessId == processId);
        var targetExists = await _dbContext.Activities
            .AnyAsync(a => a.Id == request.TargetActivityId && a.ProcessId == processId);

        if (!sourceExists)
            return BadRequest("Source activity not found in this process");
        if (!targetExists)
            return BadRequest("Target activity not found in this process");

        var edge = new ActivityEdge
        {
            ProcessId = processId,
            SourceActivityId = request.SourceActivityId,
            TargetActivityId = request.TargetActivityId,
            SourceHandle = request.SourceHandle,
            TargetHandle = request.TargetHandle,
            EdgeType = request.EdgeType,
            Label = request.Label,
            Animated = request.Animated
        };

        _dbContext.ActivityEdges.Add(edge);

        // Mark as explicit flow mode when user creates edges
        process.UseExplicitFlow = true;

        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEdges), new { organizationId, processId },
            new ActivityEdgeDto
            {
                Id = edge.Id,
                ProcessId = edge.ProcessId,
                SourceActivityId = edge.SourceActivityId,
                TargetActivityId = edge.TargetActivityId,
                SourceHandle = edge.SourceHandle,
                TargetHandle = edge.TargetHandle,
                EdgeType = edge.EdgeType,
                Label = edge.Label,
                Animated = edge.Animated,
                CreatedAt = edge.CreatedAt,
                UpdatedAt = edge.UpdatedAt
            });
    }

    /// <summary>
    /// Update an edge.
    /// </summary>
    [HttpPut("{processId}/edges/{edgeId}")]
    public async Task<ActionResult<ActivityEdgeDto>> UpdateEdge(
        Guid organizationId,
        Guid processId,
        Guid edgeId,
        [FromBody] UpdateActivityEdgeRequest request)
    {
        var edge = await _dbContext.ActivityEdges
            .Include(e => e.Process)
            .FirstOrDefaultAsync(e => e.Id == edgeId && e.ProcessId == processId && e.Process.OrganizationId == organizationId);

        if (edge == null)
            return NotFound("Edge not found");

        edge.SourceHandle = request.SourceHandle;
        edge.TargetHandle = request.TargetHandle;
        edge.EdgeType = request.EdgeType;
        edge.Label = request.Label;
        edge.Animated = request.Animated;

        await _dbContext.SaveChangesAsync();

        return Ok(new ActivityEdgeDto
        {
            Id = edge.Id,
            ProcessId = edge.ProcessId,
            SourceActivityId = edge.SourceActivityId,
            TargetActivityId = edge.TargetActivityId,
            SourceHandle = edge.SourceHandle,
            TargetHandle = edge.TargetHandle,
            EdgeType = edge.EdgeType,
            Label = edge.Label,
            Animated = edge.Animated,
            CreatedAt = edge.CreatedAt,
            UpdatedAt = edge.UpdatedAt
        });
    }

    /// <summary>
    /// Delete an edge.
    /// </summary>
    [HttpDelete("{processId}/edges/{edgeId}")]
    public async Task<IActionResult> DeleteEdge(Guid organizationId, Guid processId, Guid edgeId)
    {
        var edge = await _dbContext.ActivityEdges
            .Include(e => e.Process)
            .FirstOrDefaultAsync(e => e.Id == edgeId && e.ProcessId == processId && e.Process.OrganizationId == organizationId);

        if (edge == null)
            return NotFound("Edge not found");

        // Soft delete - CLAUDE.md compliance
        edge.SoftDelete();

        // Mark as explicit flow mode when user deletes edges (keeps the process in explicit mode)
        edge.Process.UseExplicitFlow = true;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    #endregion
}
