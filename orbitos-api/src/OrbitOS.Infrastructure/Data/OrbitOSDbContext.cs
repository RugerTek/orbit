using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Common;
using OrbitOS.Domain.Entities;

namespace OrbitOS.Infrastructure.Data;

public class OrbitOSDbContext : DbContext
{
    public OrbitOSDbContext(DbContextOptions<OrbitOSDbContext> options) : base(options)
    {
    }

    // Core entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    // System permission entities (platform access control)
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<SystemRole> SystemRoles => Set<SystemRole>();
    public DbSet<SystemRolePermission> SystemRolePermissions => Set<SystemRolePermission>();
    public DbSet<UserSystemRole> UserSystemRoles => Set<UserSystemRole>();

    // Operational entities (business work units)
    public DbSet<Function> Functions => Set<Function>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RoleFunction> RoleFunctions => Set<RoleFunction>();
    public DbSet<UserRole> UserRoles => Set<UserRole>(); // Deprecated - use UserSystemRoles

    // Resource entities
    public DbSet<ResourceSubtype> ResourceSubtypes => Set<ResourceSubtype>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<FunctionCapability> FunctionCapabilities => Set<FunctionCapability>();

    // Process entities
    public DbSet<Process> Processes => Set<Process>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityEdge> ActivityEdges => Set<ActivityEdge>();

    // Canvas entities
    public DbSet<Canvas> Canvases => Set<Canvas>();
    public DbSet<CanvasBlock> CanvasBlocks => Set<CanvasBlock>();
    public DbSet<BlockReference> BlockReferences => Set<BlockReference>();

    // Business Model Canvas entities
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ValueProposition> ValuePropositions => Set<ValueProposition>();
    public DbSet<CustomerRelationship> CustomerRelationships => Set<CustomerRelationship>();
    public DbSet<RevenueStream> RevenueStreams => Set<RevenueStream>();

    // Goal entities
    public DbSet<Goal> Goals => Set<Goal>();

    // AI entities
    public DbSet<AiAgent> AiAgents => Set<AiAgent>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<PendingAction> PendingActions => Set<PendingAction>();
    public DbSet<AssistantChatMessage> AssistantChatMessages => Set<AssistantChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AzureAdObjectId)
                .IsUnique()
                .HasFilter("[AzureAdObjectId] IS NOT NULL");
            entity.HasIndex(e => e.GoogleId)
                .IsUnique()
                .HasFilter("[GoogleId] IS NOT NULL");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.AzureAdObjectId).HasMaxLength(128);
            entity.Property(e => e.GoogleId).HasMaxLength(128);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
        });

        // Organization configuration
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Slug).HasMaxLength(128);
        });

        // OrganizationMembership configuration
        modelBuilder.Entity<OrganizationMembership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.OrganizationId }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.OrganizationMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Memberships)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Function configuration
        modelBuilder.Entity<Function>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.HasIndex(e => new { e.OrganizationId, e.Name }).IsUnique();
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Functions)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Roles)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RoleFunction (junction table) configuration
        modelBuilder.Entity<RoleFunction>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Unique constraint only for non-deleted records
            entity.HasIndex(e => new { e.RoleId, e.FunctionId })
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RoleFunctions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Function)
                .WithMany(f => f.RoleFunctions)
                .HasForeignKey(e => e.FunctionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // UserRole (junction table) configuration - DEPRECATED, use UserSystemRole
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.RoleId, e.OrganizationId }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Permission configuration (system-level permissions)
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // SystemRole configuration (system-level roles)
        modelBuilder.Entity<SystemRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // SystemRolePermission (junction table) configuration
        modelBuilder.Entity<SystemRolePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SystemRoleId, e.PermissionId }).IsUnique();
            entity.HasOne(e => e.SystemRole)
                .WithMany(r => r.SystemRolePermissions)
                .HasForeignKey(e => e.SystemRoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.SystemRolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserSystemRole (junction table) configuration
        modelBuilder.Entity<UserSystemRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.SystemRoleId, e.OrganizationId }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserSystemRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SystemRole)
                .WithMany(r => r.UserSystemRoles)
                .HasForeignKey(e => e.SystemRoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ResourceSubtype configuration
        modelBuilder.Entity<ResourceSubtype>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.HasIndex(e => new { e.OrganizationId, e.ResourceType, e.Name }).IsUnique();
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.ResourceSubtypes)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Resource configuration
        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Resources)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ResourceSubtype)
                .WithMany(s => s.Resources)
                .HasForeignKey(e => e.ResourceSubtypeId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.LinkedUser)
                .WithMany(u => u.LinkedResources)
                .HasForeignKey(e => e.LinkedUserId)
                .OnDelete(DeleteBehavior.SetNull);
            // Org chart self-reference: reporting hierarchy
            entity.HasOne(e => e.ReportsToResource)
                .WithMany(e => e.DirectReports)
                .HasForeignKey(e => e.ReportsToResourceId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade cycles
            entity.HasIndex(e => e.ReportsToResourceId)
                .HasFilter("[ReportsToResourceId] IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.IsVacant });
            entity.Property(e => e.IsVacant).HasDefaultValue(false);
            entity.Property(e => e.VacantPositionTitle).HasMaxLength(255);
        });

        // RoleAssignment configuration
        modelBuilder.Entity<RoleAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Unique constraint only for non-deleted records (supports soft delete + re-assignment)
            entity.HasIndex(e => new { e.ResourceId, e.RoleId })
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");
            entity.HasOne(e => e.Resource)
                .WithMany(r => r.RoleAssignments)
                .HasForeignKey(e => e.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RoleAssignments)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // FunctionCapability configuration
        modelBuilder.Entity<FunctionCapability>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Unique constraint only for non-deleted records (supports soft delete + re-assignment)
            entity.HasIndex(e => new { e.ResourceId, e.FunctionId })
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");
            entity.HasOne(e => e.Resource)
                .WithMany(r => r.FunctionCapabilities)
                .HasForeignKey(e => e.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Function)
                .WithMany(f => f.FunctionCapabilities)
                .HasForeignKey(e => e.FunctionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Process configuration
        modelBuilder.Entity<Process>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.HasIndex(e => new { e.OrganizationId, e.Name }).IsUnique();
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Processes)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Owner)
                .WithMany(r => r.OwnedProcesses)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.LinkedProcess)
                .WithMany()
                .HasForeignKey(e => e.LinkedProcessId)
                .OnDelete(DeleteBehavior.NoAction);
            // Entry and exit activity for flow visualization
            // Use NoAction to avoid cascade cycles with Activities->Process relationship
            entity.HasOne(e => e.EntryActivity)
                .WithMany()
                .HasForeignKey(e => e.EntryActivityId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ExitActivity)
                .WithMany()
                .HasForeignKey(e => e.ExitActivityId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Activity configuration
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.HasIndex(e => new { e.ProcessId, e.Order }).IsUnique();
            entity.HasOne(e => e.Process)
                .WithMany(p => p.Activities)
                .HasForeignKey(e => e.ProcessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Function)
                .WithMany(f => f.Activities)
                .HasForeignKey(e => e.FunctionId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.AssignedResource)
                .WithMany(r => r.AssignedActivities)
                .HasForeignKey(e => e.AssignedResourceId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.LinkedProcess)
                .WithMany()
                .HasForeignKey(e => e.LinkedProcessId)
                .OnDelete(DeleteBehavior.NoAction);
            // Position fields for Vue Flow canvas
            entity.Property(e => e.PositionX).HasDefaultValue(0.0);
            entity.Property(e => e.PositionY).HasDefaultValue(0.0);
            // Type-specific metadata stored as JSON string for IE symbol fields
            entity.Property(e => e.MetadataJson).HasColumnType("nvarchar(max)");
        });

        // ActivityEdge configuration (Vue Flow connections)
        modelBuilder.Entity<ActivityEdge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceHandle).HasMaxLength(50);
            entity.Property(e => e.TargetHandle).HasMaxLength(50);
            entity.Property(e => e.Label).HasMaxLength(100);
            // Unique index: prevent duplicate edges between same activities with same handle
            entity.HasIndex(e => new { e.ProcessId, e.SourceActivityId, e.TargetActivityId, e.SourceHandle })
                .IsUnique();
            entity.HasOne(e => e.Process)
                .WithMany(p => p.Edges)
                .HasForeignKey(e => e.ProcessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SourceActivity)
                .WithMany(a => a.OutgoingEdges)
                .HasForeignKey(e => e.SourceActivityId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.TargetActivity)
                .WithMany(a => a.IncomingEdges)
                .HasForeignKey(e => e.TargetActivityId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Canvas configuration
        modelBuilder.Entity<Canvas>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.HasIndex(e => new { e.OrganizationId, e.Slug })
                .IsUnique()
                .HasFilter("[Slug] IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.ScopeType });
            entity.HasIndex(e => e.ParentCanvasId).HasFilter("[ParentCanvasId] IS NOT NULL");
            entity.HasIndex(e => e.ProductId).HasFilter("[ProductId] IS NOT NULL");
            entity.HasIndex(e => e.SegmentId).HasFilter("[SegmentId] IS NOT NULL");
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Canvases)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ParentCanvas)
                .WithMany(c => c.ChildCanvases)
                .HasForeignKey(e => e.ParentCanvasId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Canvases)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Segment)
                .WithMany(s => s.Canvases)
                .HasForeignKey(e => e.SegmentId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // CanvasBlock configuration
        modelBuilder.Entity<CanvasBlock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(255);
            // Unique constraint only for non-deleted records
            entity.HasIndex(e => new { e.CanvasId, e.BlockType })
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => e.OrganizationId);
            entity.HasOne(e => e.Canvas)
                .WithMany(c => c.Blocks)
                .HasForeignKey(e => e.CanvasId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.CanvasBlocks)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // BlockReference configuration
        modelBuilder.Entity<BlockReference>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Unique constraint only for non-deleted records
            entity.HasIndex(e => new { e.CanvasBlockId, e.EntityType, e.EntityId })
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.OrganizationId);
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.BlockReferences)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.CanvasBlock)
                .WithMany(b => b.References)
                .HasForeignKey(e => e.CanvasBlockId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Partner configuration
        modelBuilder.Entity<Partner>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.Website).HasMaxLength(2048);
            entity.HasIndex(e => new { e.OrganizationId, e.Slug })
                .IsUnique()
                .HasFilter("[Slug] IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.Type });
            entity.HasIndex(e => new { e.OrganizationId, e.Status });
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Partners)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Channel configuration
        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.HasIndex(e => new { e.OrganizationId, e.Slug })
                .IsUnique()
                .HasFilter("[Slug] IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.Type });
            entity.HasIndex(e => new { e.OrganizationId, e.Category });
            entity.HasIndex(e => new { e.OrganizationId, e.Status });
            entity.HasIndex(e => e.PartnerId).HasFilter("[PartnerId] IS NOT NULL");
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Channels)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Partner)
                .WithMany(p => p.Channels)
                .HasForeignKey(e => e.PartnerId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ValueProposition configuration
        modelBuilder.Entity<ValueProposition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.Headline).HasMaxLength(500);
            entity.HasIndex(e => new { e.OrganizationId, e.Slug })
                .IsUnique()
                .HasFilter("[Slug] IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.Status });
            entity.HasIndex(e => e.ProductId).HasFilter("[ProductId] IS NOT NULL");
            entity.HasIndex(e => e.SegmentId).HasFilter("[SegmentId] IS NOT NULL");
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.ValuePropositions)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.ValuePropositions)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Segment)
                .WithMany(s => s.ValuePropositions)
                .HasForeignKey(e => e.SegmentId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // CustomerRelationship configuration
        modelBuilder.Entity<CustomerRelationship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.HasIndex(e => new { e.OrganizationId, e.Slug })
                .IsUnique()
                .HasFilter("[Slug] IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.Type });
            entity.HasIndex(e => new { e.OrganizationId, e.Status });
            entity.HasIndex(e => e.SegmentId).HasFilter("[SegmentId] IS NOT NULL");
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.CustomerRelationships)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Segment)
                .WithMany(s => s.CustomerRelationships)
                .HasForeignKey(e => e.SegmentId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // RevenueStream configuration
        modelBuilder.Entity<RevenueStream>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.HasIndex(e => new { e.OrganizationId, e.Slug })
                .IsUnique()
                .HasFilter("[Slug] IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.Type });
            entity.HasIndex(e => new { e.OrganizationId, e.Status });
            entity.HasIndex(e => e.ProductId).HasFilter("[ProductId] IS NOT NULL");
            entity.HasIndex(e => e.SegmentId).HasFilter("[SegmentId] IS NOT NULL");
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.RevenueStreams)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.RevenueStreams)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Segment)
                .WithMany(s => s.RevenueStreams)
                .HasForeignKey(e => e.SegmentId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Goal configuration
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.TargetValue).HasPrecision(18, 4);
            entity.Property(e => e.CurrentValue).HasPrecision(18, 4);
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Goals)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Parent)
                .WithMany(g => g.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Owner)
                .WithMany(r => r.OwnedGoals)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // RoleAssignment precision
        modelBuilder.Entity<RoleAssignment>(entity =>
        {
            entity.Property(e => e.AllocationPercentage).HasPrecision(5, 2);
        });

        // AiAgent configuration
        modelBuilder.Entity<AiAgent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.RoleTitle).HasMaxLength(100);
            entity.Property(e => e.AvatarUrl).HasMaxLength(2048);
            entity.Property(e => e.AvatarColor).HasMaxLength(7);
            entity.Property(e => e.ModelId).HasMaxLength(100);
            entity.Property(e => e.ModelDisplayName).HasMaxLength(50);
            entity.Property(e => e.Temperature).HasPrecision(3, 2);

            // A2A fields
            entity.Property(e => e.AgentType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.SpecialistKey).HasMaxLength(50);
            entity.Property(e => e.ContextScopesJson).HasColumnType("text");
            entity.Property(e => e.BasePrompt).HasColumnType("text");
            entity.Property(e => e.CustomInstructions).HasColumnType("text");

            entity.HasIndex(e => new { e.OrganizationId, e.Name }).IsUnique();
            entity.HasIndex(e => new { e.OrganizationId, e.AgentType }); // For filtering by type
            entity.HasIndex(e => new { e.OrganizationId, e.SpecialistKey })
                .HasFilter("[SpecialistKey] IS NOT NULL"); // For finding specialists

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.AiAgents)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Conversation configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Mode).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => new { e.OrganizationId, e.Status });
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.LastMessageAt).IsDescending();
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Conversations)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SenderType).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.ModelUsed).HasMaxLength(100);
            entity.Property(e => e.MentionedAgentIdsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ReferencedItemsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.SourcesJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => new { e.ConversationId, e.SequenceNumber });
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
            entity.HasIndex(e => e.SenderUserId).HasFilter("[SenderUserId] IS NOT NULL");
            entity.HasIndex(e => e.SenderAiAgentId).HasFilter("[SenderAiAgentId] IS NOT NULL");
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SenderUser)
                .WithMany()
                .HasForeignKey(e => e.SenderUserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.SenderAiAgent)
                .WithMany()
                .HasForeignKey(e => e.SenderAiAgentId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(e => e.ParentMessageId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ConversationParticipant configuration
        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParticipantType).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.UserId).HasFilter("[UserId] IS NOT NULL");
            entity.HasIndex(e => e.AiAgentId).HasFilter("[AiAgentId] IS NOT NULL");
            entity.HasIndex(e => new { e.ConversationId, e.UserId })
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");
            entity.HasIndex(e => new { e.ConversationId, e.AiAgentId })
                .IsUnique()
                .HasFilter("[AiAgentId] IS NOT NULL");
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.AiAgent)
                .WithMany()
                .HasForeignKey(e => e.AiAgentId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.LastSeenMessage)
                .WithMany()
                .HasForeignKey(e => e.LastSeenMessageId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // PendingAction configuration
        modelBuilder.Entity<PendingAction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActionType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.EntityName).HasMaxLength(255);
            entity.Property(e => e.ProposedDataJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PreviousDataJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.UserModificationsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.FinalDataJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ExecutionResultJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => new { e.OrganizationId, e.Status });
            entity.HasIndex(e => new { e.ConversationId, e.Status })
                .HasFilter("[ConversationId] IS NOT NULL");
            entity.HasIndex(e => e.MessageId).HasFilter("[MessageId] IS NOT NULL");
            entity.HasIndex(e => e.AgentId).HasFilter("[AgentId] IS NOT NULL");
            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasFilter("[EntityId] IS NOT NULL");
            entity.HasIndex(e => e.ExpiresAt).HasFilter("[ExpiresAt] IS NOT NULL AND [Status] = 'Pending'");
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Conversation)
                .WithMany()
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Message)
                .WithMany()
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Agent)
                .WithMany()
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ReviewedByUser)
                .WithMany()
                .HasForeignKey(e => e.ReviewedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // AssistantChatMessage configuration (floating AI assistant history)
        modelBuilder.Entity<AssistantChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.HasIndex(e => new { e.OrganizationId, e.UserId });
            entity.HasIndex(e => new { e.OrganizationId, e.UserId, e.SequenceNumber });
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Apply soft delete query filters to all entities
        ApplySoftDeleteFilters(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Applies global query filters to exclude soft-deleted entities.
    /// Call this method at the end of OnModelCreating.
    /// CLAUDE.md Compliance: Soft delete support for all BaseEntity types.
    /// </summary>
    private void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        // Apply soft delete filter to all entities that inherit from BaseEntity
        modelBuilder.Entity<Function>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Role>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ResourceSubtype>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Resource>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<RoleAssignment>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<FunctionCapability>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Process>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Activity>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ActivityEdge>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Canvas>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<CanvasBlock>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<BlockReference>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Partner>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Channel>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ValueProposition>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<CustomerRelationship>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<RevenueStream>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Goal>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<AiAgent>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Conversation>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Message>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ConversationParticipant>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<PendingAction>().HasQueryFilter(e => e.DeletedAt == null);

        // Core entities
        modelBuilder.Entity<User>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Organization>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<OrganizationMembership>().HasQueryFilter(e => e.DeletedAt == null);

        // System permission entities
        modelBuilder.Entity<Permission>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<SystemRole>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<SystemRolePermission>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<UserSystemRole>().HasQueryFilter(e => e.DeletedAt == null);

        // Junction tables
        modelBuilder.Entity<RoleFunction>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<UserRole>().HasQueryFilter(e => e.DeletedAt == null);

        // Additional entities
        modelBuilder.Entity<Product>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Segment>().HasQueryFilter(e => e.DeletedAt == null);
    }
}
