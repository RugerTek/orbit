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

    // Goal entities
    public DbSet<Goal> Goals => Set<Goal>();

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
            entity.HasIndex(e => new { e.RoleId, e.FunctionId }).IsUnique();
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
        });

        // RoleAssignment configuration
        modelBuilder.Entity<RoleAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ResourceId, e.RoleId }).IsUnique();
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
            entity.HasIndex(e => new { e.ResourceId, e.FunctionId }).IsUnique();
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
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Canvases)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CanvasBlock configuration
        modelBuilder.Entity<CanvasBlock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CanvasId, e.BlockType }).IsUnique();
            entity.HasOne(e => e.Canvas)
                .WithMany(c => c.Blocks)
                .HasForeignKey(e => e.CanvasId)
                .OnDelete(DeleteBehavior.Cascade);
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
}
