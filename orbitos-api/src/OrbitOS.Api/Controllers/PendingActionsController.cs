using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId}/pending-actions")]
[AllowAnonymous] // TODO: Re-enable authorization
public class PendingActionsController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<PendingActionsController> _logger;

    public PendingActionsController(OrbitOSDbContext dbContext, ILogger<PendingActionsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// List pending actions for an organization with optional filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PendingActionsListResponse>> GetPendingActions(
        Guid organizationId,
        [FromQuery] Guid? conversationId = null,
        [FromQuery] PendingActionStatus? status = null,
        [FromQuery] string? entityType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PendingActions
            .Include(pa => pa.Agent)
            .Include(pa => pa.ReviewedByUser)
            .Where(pa => pa.OrganizationId == organizationId);

        if (conversationId.HasValue)
            query = query.Where(pa => pa.ConversationId == conversationId);

        if (status.HasValue)
            query = query.Where(pa => pa.Status == status);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(pa => pa.EntityType == entityType);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(pa => pa.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pa => new PendingActionDto
            {
                Id = pa.Id,
                ActionType = pa.ActionType.ToString(),
                EntityType = pa.EntityType,
                EntityId = pa.EntityId,
                EntityName = pa.EntityName,
                ProposedData = pa.ProposedDataJson,
                PreviousData = pa.PreviousDataJson,
                Reason = pa.Reason,
                Status = pa.Status.ToString(),
                AgentId = pa.AgentId,
                AgentName = pa.Agent != null ? pa.Agent.Name : null,
                ConversationId = pa.ConversationId,
                ReviewedByUserName = pa.ReviewedByUser != null ? pa.ReviewedByUser.DisplayName : null,
                ReviewedAt = pa.ReviewedAt,
                ExecutedAt = pa.ExecutedAt,
                CreatedAt = pa.CreatedAt,
                ExpiresAt = pa.ExpiresAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new PendingActionsListResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get a specific pending action with full details
    /// </summary>
    [HttpGet("{actionId}")]
    public async Task<ActionResult<PendingActionDetailDto>> GetPendingAction(
        Guid organizationId,
        Guid actionId,
        CancellationToken cancellationToken = default)
    {
        var action = await _dbContext.PendingActions
            .Include(pa => pa.Agent)
            .Include(pa => pa.ReviewedByUser)
            .FirstOrDefaultAsync(pa => pa.Id == actionId && pa.OrganizationId == organizationId, cancellationToken);

        if (action == null)
            return NotFound(new { message = "Pending action not found" });

        return Ok(new PendingActionDetailDto
        {
            Id = action.Id,
            ActionType = action.ActionType.ToString(),
            EntityType = action.EntityType,
            EntityId = action.EntityId,
            EntityName = action.EntityName,
            ProposedData = action.ProposedDataJson,
            PreviousData = action.PreviousDataJson,
            Reason = action.Reason,
            Status = action.Status.ToString(),
            AgentId = action.AgentId,
            AgentName = action.Agent?.Name,
            ConversationId = action.ConversationId,
            MessageId = action.MessageId,
            UserModifications = action.UserModificationsJson,
            FinalData = action.FinalDataJson,
            ExecutionResult = action.ExecutionResultJson,
            ResultEntityId = action.ResultEntityId,
            ReviewedByUserId = action.ReviewedByUserId,
            ReviewedByUserName = action.ReviewedByUser?.DisplayName,
            ReviewedAt = action.ReviewedAt,
            RejectionReason = action.RejectionReason,
            ExecutedAt = action.ExecutedAt,
            ExpiresAt = action.ExpiresAt,
            CreatedAt = action.CreatedAt
        });
    }

    /// <summary>
    /// Approve a pending action, optionally with modifications
    /// </summary>
    [HttpPost("{actionId}/approve")]
    public async Task<ActionResult<PendingActionResultDto>> ApprovePendingAction(
        Guid organizationId,
        Guid actionId,
        [FromBody] ApproveActionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var action = await _dbContext.PendingActions
            .FirstOrDefaultAsync(pa => pa.Id == actionId && pa.OrganizationId == organizationId, cancellationToken);

        if (action == null)
            return NotFound(new { message = "Pending action not found" });

        if (action.Status != PendingActionStatus.Pending)
            return BadRequest(new { message = $"Cannot approve action with status '{action.Status}'" });

        if (action.ExpiresAt.HasValue && action.ExpiresAt < DateTime.UtcNow)
        {
            action.Status = PendingActionStatus.Expired;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return BadRequest(new { message = "Action has expired" });
        }

        // Apply user modifications if provided
        if (!string.IsNullOrWhiteSpace(request?.Modifications))
        {
            action.UserModificationsJson = request.Modifications;
            action.Status = PendingActionStatus.Modified;

            // Merge proposed data with modifications for final data
            action.FinalDataJson = MergeJsonData(action.ProposedDataJson, request.Modifications);
        }
        else
        {
            action.Status = PendingActionStatus.Approved;
            action.FinalDataJson = action.ProposedDataJson;
        }

        action.ReviewedAt = DateTime.UtcNow;
        // TODO: Set ReviewedByUserId from authenticated user

        // Execute the action
        try
        {
            var result = await ExecuteActionAsync(organizationId, action, cancellationToken);
            action.Status = PendingActionStatus.Executed;
            action.ExecutedAt = DateTime.UtcNow;
            action.ExecutionResultJson = JsonSerializer.Serialize(new { success = true, entityId = result.EntityId, message = result.Message });
            action.ResultEntityId = result.EntityId;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new PendingActionResultDto
            {
                Success = true,
                ActionId = action.Id,
                Status = action.Status.ToString(),
                ResultEntityId = result.EntityId,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute pending action {ActionId}", actionId);
            action.Status = PendingActionStatus.Failed;
            action.ExecutionResultJson = JsonSerializer.Serialize(new { success = false, error = ex.Message });
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new PendingActionResultDto
            {
                Success = false,
                ActionId = action.Id,
                Status = action.Status.ToString(),
                Message = $"Execution failed: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Reject a pending action
    /// </summary>
    [HttpPost("{actionId}/reject")]
    public async Task<ActionResult<PendingActionResultDto>> RejectPendingAction(
        Guid organizationId,
        Guid actionId,
        [FromBody] RejectActionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var action = await _dbContext.PendingActions
            .FirstOrDefaultAsync(pa => pa.Id == actionId && pa.OrganizationId == organizationId, cancellationToken);

        if (action == null)
            return NotFound(new { message = "Pending action not found" });

        if (action.Status != PendingActionStatus.Pending)
            return BadRequest(new { message = $"Cannot reject action with status '{action.Status}'" });

        action.Status = PendingActionStatus.Rejected;
        action.RejectionReason = request?.Reason;
        action.ReviewedAt = DateTime.UtcNow;
        // TODO: Set ReviewedByUserId from authenticated user

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new PendingActionResultDto
        {
            Success = true,
            ActionId = action.Id,
            Status = action.Status.ToString(),
            Message = "Action rejected"
        });
    }

    /// <summary>
    /// Retry a failed action
    /// </summary>
    [HttpPost("{actionId}/retry")]
    public async Task<ActionResult<PendingActionResultDto>> RetryPendingAction(
        Guid organizationId,
        Guid actionId,
        CancellationToken cancellationToken = default)
    {
        var action = await _dbContext.PendingActions
            .FirstOrDefaultAsync(pa => pa.Id == actionId && pa.OrganizationId == organizationId, cancellationToken);

        if (action == null)
            return NotFound(new { message = "Pending action not found" });

        if (action.Status != PendingActionStatus.Failed)
            return BadRequest(new { message = "Only failed actions can be retried" });

        try
        {
            var result = await ExecuteActionAsync(organizationId, action, cancellationToken);
            action.Status = PendingActionStatus.Executed;
            action.ExecutedAt = DateTime.UtcNow;
            action.ExecutionResultJson = JsonSerializer.Serialize(new { success = true, entityId = result.EntityId, message = result.Message, retriedAt = DateTime.UtcNow });
            action.ResultEntityId = result.EntityId;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new PendingActionResultDto
            {
                Success = true,
                ActionId = action.Id,
                Status = action.Status.ToString(),
                ResultEntityId = result.EntityId,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry pending action {ActionId}", actionId);
            action.ExecutionResultJson = JsonSerializer.Serialize(new { success = false, error = ex.Message, retriedAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new PendingActionResultDto
            {
                Success = false,
                ActionId = action.Id,
                Status = action.Status.ToString(),
                Message = $"Retry failed: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get count of pending actions by status
    /// </summary>
    [HttpGet("counts")]
    public async Task<ActionResult<PendingActionCountsDto>> GetPendingActionCounts(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var counts = await _dbContext.PendingActions
            .Where(pa => pa.OrganizationId == organizationId)
            .GroupBy(pa => pa.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return Ok(new PendingActionCountsDto
        {
            Pending = counts.FirstOrDefault(c => c.Status == PendingActionStatus.Pending)?.Count ?? 0,
            Approved = counts.FirstOrDefault(c => c.Status == PendingActionStatus.Approved)?.Count ?? 0,
            Rejected = counts.FirstOrDefault(c => c.Status == PendingActionStatus.Rejected)?.Count ?? 0,
            Executed = counts.FirstOrDefault(c => c.Status == PendingActionStatus.Executed)?.Count ?? 0,
            Failed = counts.FirstOrDefault(c => c.Status == PendingActionStatus.Failed)?.Count ?? 0,
            Expired = counts.FirstOrDefault(c => c.Status == PendingActionStatus.Expired)?.Count ?? 0
        });
    }

    private string MergeJsonData(string originalJson, string modificationsJson)
    {
        try
        {
            using var originalDoc = JsonDocument.Parse(originalJson);
            using var modDoc = JsonDocument.Parse(modificationsJson);

            var merged = new Dictionary<string, JsonElement>();

            // Add all original properties
            foreach (var prop in originalDoc.RootElement.EnumerateObject())
            {
                merged[prop.Name] = prop.Value.Clone();
            }

            // Override with modifications
            foreach (var prop in modDoc.RootElement.EnumerateObject())
            {
                merged[prop.Name] = prop.Value.Clone();
            }

            return JsonSerializer.Serialize(merged);
        }
        catch
        {
            // If merge fails, return modifications as the final data
            return modificationsJson;
        }
    }

    private async Task<ExecutionResult> ExecuteActionAsync(Guid organizationId, PendingAction action, CancellationToken cancellationToken)
    {
        var dataJson = action.FinalDataJson ?? action.ProposedDataJson;
        using var dataDoc = JsonDocument.Parse(dataJson);
        var data = dataDoc.RootElement;

        return action.ActionType switch
        {
            ActionType.Create => await ExecuteCreateAsync(organizationId, action.EntityType, data, cancellationToken),
            ActionType.Update => await ExecuteUpdateAsync(organizationId, action.EntityType, action.EntityId!.Value, data, cancellationToken),
            ActionType.Delete => await ExecuteDeleteAsync(organizationId, action.EntityType, action.EntityId!.Value, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown action type: {action.ActionType}")
        };
    }

    private async Task<ExecutionResult> ExecuteCreateAsync(Guid organizationId, string entityType, JsonElement data, CancellationToken cancellationToken)
    {
        return entityType switch
        {
            "Partner" => await CreatePartnerAsync(organizationId, data, cancellationToken),
            "Channel" => await CreateChannelAsync(organizationId, data, cancellationToken),
            "ValueProposition" => await CreateValuePropositionAsync(organizationId, data, cancellationToken),
            "CustomerRelationship" => await CreateCustomerRelationshipAsync(organizationId, data, cancellationToken),
            "RevenueStream" => await CreateRevenueStreamAsync(organizationId, data, cancellationToken),
            "Function" => await CreateFunctionAsync(organizationId, data, cancellationToken),
            "Role" => await CreateRoleAsync(organizationId, data, cancellationToken),
            "Process" => await CreateProcessAsync(organizationId, data, cancellationToken),
            "Goal" => await CreateGoalAsync(organizationId, data, cancellationToken),
            "Resource" => await CreateResourceAsync(organizationId, data, cancellationToken),
            _ => throw new InvalidOperationException($"Create not implemented for entity type: {entityType}")
        };
    }

    private async Task<ExecutionResult> ExecuteUpdateAsync(Guid organizationId, string entityType, Guid entityId, JsonElement data, CancellationToken cancellationToken)
    {
        return entityType switch
        {
            "Partner" => await UpdatePartnerAsync(organizationId, entityId, data, cancellationToken),
            "Channel" => await UpdateChannelAsync(organizationId, entityId, data, cancellationToken),
            "ValueProposition" => await UpdateValuePropositionAsync(organizationId, entityId, data, cancellationToken),
            "CustomerRelationship" => await UpdateCustomerRelationshipAsync(organizationId, entityId, data, cancellationToken),
            "RevenueStream" => await UpdateRevenueStreamAsync(organizationId, entityId, data, cancellationToken),
            "Function" => await UpdateFunctionAsync(organizationId, entityId, data, cancellationToken),
            "Role" => await UpdateRoleAsync(organizationId, entityId, data, cancellationToken),
            "Process" => await UpdateProcessAsync(organizationId, entityId, data, cancellationToken),
            "Goal" => await UpdateGoalAsync(organizationId, entityId, data, cancellationToken),
            "Resource" => await UpdateResourceAsync(organizationId, entityId, data, cancellationToken),
            _ => throw new InvalidOperationException($"Update not implemented for entity type: {entityType}")
        };
    }

    private async Task<ExecutionResult> ExecuteDeleteAsync(Guid organizationId, string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        return entityType switch
        {
            "Partner" => await DeleteEntityAsync<Partner>(organizationId, entityId, cancellationToken),
            "Channel" => await DeleteEntityAsync<Channel>(organizationId, entityId, cancellationToken),
            "ValueProposition" => await DeleteEntityAsync<ValueProposition>(organizationId, entityId, cancellationToken),
            "CustomerRelationship" => await DeleteEntityAsync<CustomerRelationship>(organizationId, entityId, cancellationToken),
            "RevenueStream" => await DeleteEntityAsync<RevenueStream>(organizationId, entityId, cancellationToken),
            "Function" => await DeleteEntityAsync<Function>(organizationId, entityId, cancellationToken),
            "Role" => await DeleteEntityAsync<Role>(organizationId, entityId, cancellationToken),
            "Process" => await DeleteEntityAsync<Process>(organizationId, entityId, cancellationToken),
            "Goal" => await DeleteEntityAsync<Goal>(organizationId, entityId, cancellationToken),
            "Resource" => await DeleteEntityAsync<Resource>(organizationId, entityId, cancellationToken),
            _ => throw new InvalidOperationException($"Delete not implemented for entity type: {entityType}")
        };
    }

    // Helper method for soft deleting any entity
    private async Task<ExecutionResult> DeleteEntityAsync<T>(Guid organizationId, Guid entityId, CancellationToken cancellationToken) where T : class
    {
        var entity = await _dbContext.Set<T>().FindAsync([entityId], cancellationToken);
        if (entity == null)
            throw new InvalidOperationException($"Entity not found: {entityId}");

        // Soft delete
        if (entity is OrbitOS.Domain.Common.BaseEntity baseEntity)
        {
            baseEntity.DeletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new ExecutionResult { EntityId = entityId, Message = "Entity deleted successfully" };
        }

        throw new InvalidOperationException("Entity does not support soft delete");
    }

    // Create implementations for each entity type
    private async Task<ExecutionResult> CreatePartnerAsync(Guid organizationId, JsonElement data, CancellationToken cancellationToken)
    {
        var partner = new Partner
        {
            OrganizationId = organizationId,
            Name = data.GetProperty("name").GetString()!,
            Description = data.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Type = Enum.TryParse<PartnerType>(data.TryGetProperty("type", out var type) ? type.GetString() : "Strategic", true, out var pt) ? pt : PartnerType.Strategic,
            StrategicValue = Enum.TryParse<StrategicValue>(data.TryGetProperty("strategicValue", out var sv) ? sv.GetString() : "Medium", true, out var svp) ? svp : StrategicValue.Medium,
            Status = PartnerStatus.Active
        };
        _dbContext.Partners.Add(partner);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = partner.Id, Message = $"Created partner: {partner.Name}" };
    }

    private async Task<ExecutionResult> CreateChannelAsync(Guid organizationId, JsonElement data, CancellationToken cancellationToken)
    {
        var channel = new Channel
        {
            OrganizationId = organizationId,
            Name = data.GetProperty("name").GetString()!,
            Description = data.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Type = Enum.TryParse<ChannelType>(data.TryGetProperty("type", out var type) ? type.GetString() : "Direct", true, out var ct) ? ct : ChannelType.Direct,
            Category = Enum.TryParse<ChannelCategory>(data.TryGetProperty("category", out var cat) ? cat.GetString() : "Sales", true, out var cc) ? cc : ChannelCategory.Sales,
            Ownership = Enum.TryParse<ChannelOwnership>(data.TryGetProperty("ownership", out var own) ? own.GetString() : "Owned", true, out var co) ? co : ChannelOwnership.Owned,
            Status = ChannelStatus.Active
        };
        _dbContext.Channels.Add(channel);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = channel.Id, Message = $"Created channel: {channel.Name}" };
    }

    private async Task<ExecutionResult> CreateValuePropositionAsync(Guid organizationId, JsonElement data, CancellationToken cancellationToken)
    {
        var vp = new ValueProposition
        {
            OrganizationId = organizationId,
            Name = data.GetProperty("name").GetString()!,
            Headline = data.TryGetProperty("headline", out var hl) ? hl.GetString()! : data.GetProperty("name").GetString()!,
            Description = data.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Status = ValuePropositionStatus.Active
        };
        _dbContext.ValuePropositions.Add(vp);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = vp.Id, Message = $"Created value proposition: {vp.Name}" };
    }

    private async Task<ExecutionResult> CreateCustomerRelationshipAsync(Guid organizationId, JsonElement data, CancellationToken cancellationToken)
    {
        var cr = new CustomerRelationship
        {
            OrganizationId = organizationId,
            Name = data.GetProperty("name").GetString()!,
            Description = data.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Type = Enum.TryParse<CustomerRelationshipType>(data.TryGetProperty("type", out var type) ? type.GetString() : "PersonalAssistance", true, out var crt) ? crt : CustomerRelationshipType.PersonalAssistance,
            Status = CustomerRelationshipStatus.Active
        };
        _dbContext.CustomerRelationships.Add(cr);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = cr.Id, Message = $"Created customer relationship: {cr.Name}" };
    }

    private async Task<ExecutionResult> CreateRevenueStreamAsync(Guid organizationId, JsonElement data, CancellationToken cancellationToken)
    {
        var rs = new RevenueStream
        {
            OrganizationId = organizationId,
            Name = data.GetProperty("name").GetString()!,
            Description = data.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Type = Enum.TryParse<RevenueStreamType>(data.TryGetProperty("type", out var type) ? type.GetString() : "Subscription", true, out var rst) ? rst : RevenueStreamType.Subscription,
            PricingMechanism = Enum.TryParse<PricingMechanism>(data.TryGetProperty("pricingMechanism", out var pm) ? pm.GetString() : "Fixed", true, out var pmv) ? pmv : PricingMechanism.Fixed,
            Status = RevenueStreamStatus.Active
        };
        _dbContext.RevenueStreams.Add(rs);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = rs.Id, Message = $"Created revenue stream: {rs.Name}" };
    }

    private async Task<ExecutionResult> CreateFunctionAsync(Guid organizationId, JsonElement data, CancellationToken cancellationToken)
    {
        var func = new Function
        {
            OrganizationId = organizationId,
            Name = data.GetProperty("name").GetString()!,
            Description = data.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Purpose = data.TryGetProperty("purpose", out var purpose) ? purpose.GetString() : null,
            Category = data.TryGetProperty("category", out var cat) ? cat.GetString() : null,
            Status = FunctionStatus.Active
        };
        _dbContext.Functions.Add(func);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = func.Id, Message = $"Created function: {func.Name}" };
    }

    private async Task<ExecutionResult> CreateRoleAsync(Guid organizationId, JsonElement data, CancellationToken cancellationToken)
    {
        var role = new Role
        {
            OrganizationId = organizationId,
            Name = data.GetProperty("name").GetString()!,
            Description = data.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Department = data.TryGetProperty("department", out var dept) ? dept.GetString() : null
        };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = role.Id, Message = $"Created role: {role.Name}" };
    }

    private async Task<ExecutionResult> CreateProcessAsync(Guid organizationId, JsonElement data, CancellationToken cancellationToken)
    {
        var process = new Process
        {
            OrganizationId = organizationId,
            Name = data.GetProperty("name").GetString()!,
            Description = data.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Purpose = data.TryGetProperty("purpose", out var purpose) ? purpose.GetString() : null,
            Trigger = data.TryGetProperty("trigger", out var trigger) ? trigger.GetString() : null,
            Output = data.TryGetProperty("output", out var output) ? output.GetString() : null,
            Frequency = Enum.TryParse<ProcessFrequency>(data.TryGetProperty("frequency", out var freq) ? freq.GetString() : "OnDemand", true, out var pf) ? pf : ProcessFrequency.OnDemand,
            Status = ProcessStatus.Draft,
            StateType = ProcessStateType.Current
        };
        _dbContext.Processes.Add(process);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = process.Id, Message = $"Created process: {process.Name}" };
    }

    private async Task<ExecutionResult> CreateGoalAsync(Guid organizationId, JsonElement data, CancellationToken cancellationToken)
    {
        var goal = new Goal
        {
            OrganizationId = organizationId,
            Name = data.GetProperty("name").GetString()!,
            Description = data.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            GoalType = Enum.TryParse<GoalType>(data.TryGetProperty("goalType", out var gt) ? gt.GetString() : "Objective", true, out var gtp) ? gtp : GoalType.Objective,
            TargetValue = data.TryGetProperty("targetValue", out var tv) ? tv.GetDecimal() : null,
            Unit = data.TryGetProperty("unit", out var unit) ? unit.GetString() : null,
            Status = GoalStatus.Active
        };
        _dbContext.Goals.Add(goal);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = goal.Id, Message = $"Created goal: {goal.Name}" };
    }

    private async Task<ExecutionResult> CreateResourceAsync(Guid organizationId, JsonElement data, CancellationToken cancellationToken)
    {
        Guid? subtypeId = data.TryGetProperty("resourceSubtypeId", out var stid) ? Guid.Parse(stid.GetString()!) : null;

        // If no subtype specified, try to find a default person subtype
        if (!subtypeId.HasValue)
        {
            var defaultSubtype = await _dbContext.ResourceSubtypes
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.ResourceType == ResourceType.Person, cancellationToken);
            subtypeId = defaultSubtype?.Id;
        }

        if (!subtypeId.HasValue)
            throw new InvalidOperationException("No resource subtype found for person");

        var resource = new Resource
        {
            OrganizationId = organizationId,
            ResourceSubtypeId = subtypeId.Value,
            Name = data.GetProperty("name").GetString()!,
            Description = data.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Status = ResourceStatus.Active
        };
        _dbContext.Resources.Add(resource);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = resource.Id, Message = $"Created resource: {resource.Name}" };
    }

    // Update implementations
    private async Task<ExecutionResult> UpdatePartnerAsync(Guid organizationId, Guid entityId, JsonElement data, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Partners.FirstOrDefaultAsync(e => e.Id == entityId && e.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Partner not found");

        if (data.TryGetProperty("name", out var name)) entity.Name = name.GetString()!;
        if (data.TryGetProperty("description", out var desc)) entity.Description = desc.GetString();
        if (data.TryGetProperty("type", out var type) && Enum.TryParse<PartnerType>(type.GetString(), true, out var pt)) entity.Type = pt;
        if (data.TryGetProperty("strategicValue", out var sv) && Enum.TryParse<StrategicValue>(sv.GetString(), true, out var svp)) entity.StrategicValue = svp;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = entity.Id, Message = $"Updated partner: {entity.Name}" };
    }

    private async Task<ExecutionResult> UpdateChannelAsync(Guid organizationId, Guid entityId, JsonElement data, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Channels.FirstOrDefaultAsync(e => e.Id == entityId && e.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Channel not found");

        if (data.TryGetProperty("name", out var name)) entity.Name = name.GetString()!;
        if (data.TryGetProperty("description", out var desc)) entity.Description = desc.GetString();
        if (data.TryGetProperty("type", out var type) && Enum.TryParse<ChannelType>(type.GetString(), true, out var ct)) entity.Type = ct;
        if (data.TryGetProperty("category", out var cat) && Enum.TryParse<ChannelCategory>(cat.GetString(), true, out var cc)) entity.Category = cc;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = entity.Id, Message = $"Updated channel: {entity.Name}" };
    }

    private async Task<ExecutionResult> UpdateValuePropositionAsync(Guid organizationId, Guid entityId, JsonElement data, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ValuePropositions.FirstOrDefaultAsync(e => e.Id == entityId && e.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Value proposition not found");

        if (data.TryGetProperty("name", out var name)) entity.Name = name.GetString()!;
        if (data.TryGetProperty("headline", out var hl)) entity.Headline = hl.GetString()!;
        if (data.TryGetProperty("description", out var desc)) entity.Description = desc.GetString();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = entity.Id, Message = $"Updated value proposition: {entity.Name}" };
    }

    private async Task<ExecutionResult> UpdateCustomerRelationshipAsync(Guid organizationId, Guid entityId, JsonElement data, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.CustomerRelationships.FirstOrDefaultAsync(e => e.Id == entityId && e.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Customer relationship not found");

        if (data.TryGetProperty("name", out var name)) entity.Name = name.GetString()!;
        if (data.TryGetProperty("description", out var desc)) entity.Description = desc.GetString();
        if (data.TryGetProperty("type", out var type) && Enum.TryParse<CustomerRelationshipType>(type.GetString(), true, out var crt)) entity.Type = crt;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = entity.Id, Message = $"Updated customer relationship: {entity.Name}" };
    }

    private async Task<ExecutionResult> UpdateRevenueStreamAsync(Guid organizationId, Guid entityId, JsonElement data, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.RevenueStreams.FirstOrDefaultAsync(e => e.Id == entityId && e.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Revenue stream not found");

        if (data.TryGetProperty("name", out var name)) entity.Name = name.GetString()!;
        if (data.TryGetProperty("description", out var desc)) entity.Description = desc.GetString();
        if (data.TryGetProperty("type", out var type) && Enum.TryParse<RevenueStreamType>(type.GetString(), true, out var rst)) entity.Type = rst;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = entity.Id, Message = $"Updated revenue stream: {entity.Name}" };
    }

    private async Task<ExecutionResult> UpdateFunctionAsync(Guid organizationId, Guid entityId, JsonElement data, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Functions.FirstOrDefaultAsync(e => e.Id == entityId && e.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Function not found");

        if (data.TryGetProperty("name", out var name)) entity.Name = name.GetString()!;
        if (data.TryGetProperty("description", out var desc)) entity.Description = desc.GetString();
        if (data.TryGetProperty("purpose", out var purpose)) entity.Purpose = purpose.GetString();
        if (data.TryGetProperty("category", out var cat)) entity.Category = cat.GetString();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = entity.Id, Message = $"Updated function: {entity.Name}" };
    }

    private async Task<ExecutionResult> UpdateRoleAsync(Guid organizationId, Guid entityId, JsonElement data, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Roles.FirstOrDefaultAsync(e => e.Id == entityId && e.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found");

        if (data.TryGetProperty("name", out var name)) entity.Name = name.GetString()!;
        if (data.TryGetProperty("description", out var desc)) entity.Description = desc.GetString();
        if (data.TryGetProperty("department", out var dept)) entity.Department = dept.GetString();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = entity.Id, Message = $"Updated role: {entity.Name}" };
    }

    private async Task<ExecutionResult> UpdateProcessAsync(Guid organizationId, Guid entityId, JsonElement data, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Processes.FirstOrDefaultAsync(e => e.Id == entityId && e.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Process not found");

        if (data.TryGetProperty("name", out var name)) entity.Name = name.GetString()!;
        if (data.TryGetProperty("description", out var desc)) entity.Description = desc.GetString();
        if (data.TryGetProperty("purpose", out var purpose)) entity.Purpose = purpose.GetString();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = entity.Id, Message = $"Updated process: {entity.Name}" };
    }

    private async Task<ExecutionResult> UpdateGoalAsync(Guid organizationId, Guid entityId, JsonElement data, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Goals.FirstOrDefaultAsync(e => e.Id == entityId && e.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Goal not found");

        if (data.TryGetProperty("name", out var name)) entity.Name = name.GetString()!;
        if (data.TryGetProperty("description", out var desc)) entity.Description = desc.GetString();
        if (data.TryGetProperty("targetValue", out var tv)) entity.TargetValue = tv.GetDecimal();
        if (data.TryGetProperty("currentValue", out var cv)) entity.CurrentValue = cv.GetDecimal();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = entity.Id, Message = $"Updated goal: {entity.Name}" };
    }

    private async Task<ExecutionResult> UpdateResourceAsync(Guid organizationId, Guid entityId, JsonElement data, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Resources.FirstOrDefaultAsync(e => e.Id == entityId && e.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Resource not found");

        if (data.TryGetProperty("name", out var name)) entity.Name = name.GetString()!;
        if (data.TryGetProperty("description", out var desc)) entity.Description = desc.GetString();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ExecutionResult { EntityId = entity.Id, Message = $"Updated resource: {entity.Name}" };
    }

    private class ExecutionResult
    {
        public Guid? EntityId { get; set; }
        public required string Message { get; set; }
    }
}

// DTOs
public class PendingActionsListResponse
{
    public required List<PendingActionDto> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PendingActionDto
{
    public Guid Id { get; set; }
    public required string ActionType { get; set; }
    public required string EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityName { get; set; }
    public required string ProposedData { get; set; }
    public string? PreviousData { get; set; }
    public required string Reason { get; set; }
    public required string Status { get; set; }
    public Guid? AgentId { get; set; }
    public string? AgentName { get; set; }
    public Guid? ConversationId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class PendingActionDetailDto : PendingActionDto
{
    public Guid? MessageId { get; set; }
    public string? UserModifications { get; set; }
    public string? FinalData { get; set; }
    public string? ExecutionResult { get; set; }
    public Guid? ResultEntityId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? RejectionReason { get; set; }
}

public class ApproveActionRequest
{
    public string? Modifications { get; set; }
}

public class RejectActionRequest
{
    public string? Reason { get; set; }
}

public class PendingActionResultDto
{
    public bool Success { get; set; }
    public Guid ActionId { get; set; }
    public required string Status { get; set; }
    public Guid? ResultEntityId { get; set; }
    public string? Message { get; set; }
}

public class PendingActionCountsDto
{
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Executed { get; set; }
    public int Failed { get; set; }
    public int Expired { get; set; }
}
