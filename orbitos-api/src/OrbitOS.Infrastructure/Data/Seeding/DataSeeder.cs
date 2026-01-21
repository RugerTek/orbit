using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrbitOS.Application.Interfaces;
using OrbitOS.Domain.Constants;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Services;

namespace OrbitOS.Infrastructure.Data.Seeding;

/// <summary>
/// Seeds initial data into the database.
/// Idempotent - checks for existing data before inserting.
/// </summary>
public class DataSeeder : IDataSeeder
{
    private readonly OrbitOSDbContext _context;
    private readonly ILogger<DataSeeder> _logger;
    private readonly IHostEnvironment _environment;

    public DataSeeder(OrbitOSDbContext context, ILogger<DataSeeder> logger, IHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting data seeding...");

        // Core entities
        await SeedOrganizationsAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedOrganizationMembershipsAsync(cancellationToken);

        // System permission entities (platform access control)
        await SeedPermissionsAsync(cancellationToken);
        await SeedSystemRolesAsync(cancellationToken);
        await SeedSystemRolePermissionsAsync(cancellationToken);
        await SeedUserSystemRolesAsync(cancellationToken);

        // Operations data seeding
        await SeedResourceSubtypesAsync(cancellationToken);
        await SeedResourcesAsync(cancellationToken);
        await SeedProcessesAsync(cancellationToken);
        await SeedCanvasesAsync(cancellationToken);
        await SeedGoalsAsync(cancellationToken);

        // Development-only: Seed sample operational data
        if (_environment.IsDevelopment())
        {
            await SeedSampleOperationalDataAsync(cancellationToken);
        }

        // Seed Torus organization with full org chart
        await SeedTorusOrganizationAsync(cancellationToken);

        _logger.LogInformation("Data seeding completed successfully");
    }

    private async Task SeedOrganizationsAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records - check by ID or by Slug
        if (await _context.Organizations.IgnoreQueryFilters().AnyAsync(o => o.Id == SeedIds.Organizations.Rugertek || o.Slug == "rugertek", cancellationToken))
        {
            _logger.LogDebug("Organizations already seeded");
            return;
        }

        var organization = new Organization
        {
            Id = SeedIds.Organizations.Rugertek,
            Name = "Rugertek",
            Slug = "rugertek",
            Description = "Rugertek - AI-Native Business Solutions",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded organization: {Organization}", organization.Name);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == SeedIds.Users.Rodrigo, cancellationToken))
        {
            _logger.LogDebug("Users already seeded");
            return;
        }

        var user = new User
        {
            Id = SeedIds.Users.Rodrigo,
            Email = "rodrigo@rugertek.com",
            DisplayName = "Rodrigo Campos Cervera",
            FirstName = "Rodrigo",
            LastName = "Campos Cervera",
            PasswordHash = PasswordHasher.HashPassword("123456"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded user: {Email}", user.Email);
    }

    private async Task SeedOrganizationMembershipsAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.OrganizationMemberships.IgnoreQueryFilters().AnyAsync(m => m.Id == SeedIds.Memberships.RodrigoRugertek, cancellationToken))
        {
            _logger.LogDebug("Organization memberships already seeded");
            return;
        }

        var membership = new OrganizationMembership
        {
            Id = SeedIds.Memberships.RodrigoRugertek,
            UserId = SeedIds.Users.Rodrigo,
            OrganizationId = SeedIds.Organizations.Rugertek,
            Role = MembershipRole.Owner,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded membership for user {UserId} in organization {OrgId}", membership.UserId, membership.OrganizationId);
    }

    #region System Permission Seeding

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.Permissions.IgnoreQueryFilters().AnyAsync(p => p.Id == SeedIds.Permissions.UsersView, cancellationToken))
        {
            _logger.LogDebug("Permissions already seeded");
            return;
        }

        var permissions = GetSystemPermissions();

        _context.Permissions.AddRange(permissions);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} system permissions", permissions.Count);
    }

    private List<Permission> GetSystemPermissions()
    {
        var now = DateTime.UtcNow;

        return new List<Permission>
        {
            // User Management Permissions
            new() { Id = SeedIds.Permissions.UsersView, Name = SystemFunctions.UserManagement.ViewUsers, Description = "View user profiles and information", Category = SystemFunctions.UserManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.UsersCreate, Name = SystemFunctions.UserManagement.CreateUsers, Description = "Create new user accounts", Category = SystemFunctions.UserManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.UsersUpdate, Name = SystemFunctions.UserManagement.UpdateUsers, Description = "Update user profile information", Category = SystemFunctions.UserManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.UsersDelete, Name = SystemFunctions.UserManagement.DeleteUsers, Description = "Delete user accounts", Category = SystemFunctions.UserManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.UsersRolesManage, Name = SystemFunctions.UserManagement.ManageUserRoles, Description = "Assign and remove roles from users", Category = SystemFunctions.UserManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.UsersRolesView, Name = SystemFunctions.UserManagement.ViewUserRoles, Description = "View role assignments for users", Category = SystemFunctions.UserManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.UsersInvite, Name = SystemFunctions.UserManagement.InviteUsers, Description = "Send invitations to new users", Category = SystemFunctions.UserManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.UsersDeactivate, Name = SystemFunctions.UserManagement.DeactivateUsers, Description = "Deactivate user accounts", Category = SystemFunctions.UserManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.UsersReactivate, Name = SystemFunctions.UserManagement.ReactivateUsers, Description = "Reactivate deactivated user accounts", Category = SystemFunctions.UserManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.UsersActivityView, Name = SystemFunctions.UserManagement.ViewUserActivity, Description = "View user activity logs", Category = SystemFunctions.UserManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },

            // Organization Management Permissions
            new() { Id = SeedIds.Permissions.OrgView, Name = SystemFunctions.OrganizationManagement.ViewOrganization, Description = "View organization information", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgUpdate, Name = SystemFunctions.OrganizationManagement.UpdateOrganization, Description = "Update organization information", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgSettingsManage, Name = SystemFunctions.OrganizationManagement.ManageSettings, Description = "Manage organization settings", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgSettingsView, Name = SystemFunctions.OrganizationManagement.ViewSettings, Description = "View organization settings", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgBillingManage, Name = SystemFunctions.OrganizationManagement.ManageBilling, Description = "Manage billing and subscriptions", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgBillingView, Name = SystemFunctions.OrganizationManagement.ViewBilling, Description = "View billing information", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgIntegrationsManage, Name = SystemFunctions.OrganizationManagement.ManageIntegrations, Description = "Manage third-party integrations", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgIntegrationsView, Name = SystemFunctions.OrganizationManagement.ViewIntegrations, Description = "View integration configurations", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgApiKeysManage, Name = SystemFunctions.OrganizationManagement.ManageApiKeys, Description = "Create and revoke API keys", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgApiKeysView, Name = SystemFunctions.OrganizationManagement.ViewApiKeys, Description = "View API keys", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgWebhooksManage, Name = SystemFunctions.OrganizationManagement.ManageWebhooks, Description = "Configure webhook endpoints", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgWebhooksView, Name = SystemFunctions.OrganizationManagement.ViewWebhooks, Description = "View webhook configurations", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgAuditLogView, Name = SystemFunctions.OrganizationManagement.ViewAuditLog, Description = "View organization audit log", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.OrgDataExport, Name = SystemFunctions.OrganizationManagement.ExportData, Description = "Export organization data", Category = SystemFunctions.OrganizationManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },

            // Role Management Permissions
            new() { Id = SeedIds.Permissions.RolesView, Name = SystemFunctions.RoleManagement.ViewRoles, Description = "View roles", Category = SystemFunctions.RoleManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.RolesCreate, Name = SystemFunctions.RoleManagement.CreateRoles, Description = "Create new roles", Category = SystemFunctions.RoleManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.RolesUpdate, Name = SystemFunctions.RoleManagement.UpdateRoles, Description = "Update existing roles", Category = SystemFunctions.RoleManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.RolesDelete, Name = SystemFunctions.RoleManagement.DeleteRoles, Description = "Delete roles", Category = SystemFunctions.RoleManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.RolesFunctionsAssign, Name = SystemFunctions.RoleManagement.AssignFunctions, Description = "Assign functions to roles", Category = SystemFunctions.RoleManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },

            // Function Management Permissions
            new() { Id = SeedIds.Permissions.FunctionsView, Name = SystemFunctions.FunctionManagement.ViewFunctions, Description = "View functions", Category = SystemFunctions.FunctionManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.FunctionsCreate, Name = SystemFunctions.FunctionManagement.CreateFunctions, Description = "Create custom functions", Category = SystemFunctions.FunctionManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.FunctionsUpdate, Name = SystemFunctions.FunctionManagement.UpdateFunctions, Description = "Update functions", Category = SystemFunctions.FunctionManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Permissions.FunctionsDelete, Name = SystemFunctions.FunctionManagement.DeleteFunctions, Description = "Delete functions", Category = SystemFunctions.FunctionManagement.Category, IsBuiltIn = true, CreatedAt = now, UpdatedAt = now },
        };
    }

    private async Task SeedSystemRolesAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.SystemRoles.IgnoreQueryFilters().AnyAsync(r => r.Id == SeedIds.SystemRoles.SuperAdmin, cancellationToken))
        {
            _logger.LogDebug("System roles already seeded");
            return;
        }

        var now = DateTime.UtcNow;

        var systemRoles = new List<SystemRole>
        {
            new()
            {
                Id = SeedIds.SystemRoles.SuperAdmin,
                Name = Domain.Constants.SystemRoles.SuperAdmin,
                Description = "Full access to all system features including user and organization management",
                IsBuiltIn = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.SystemRoles.UserAdmin,
                Name = Domain.Constants.SystemRoles.UserAdmin,
                Description = "Full access to user management features",
                IsBuiltIn = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.SystemRoles.OrgAdmin,
                Name = Domain.Constants.SystemRoles.OrgAdmin,
                Description = "Full access to organization management features",
                IsBuiltIn = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.SystemRoles.Viewer,
                Name = Domain.Constants.SystemRoles.Viewer,
                Description = "Read-only access to view users and organization information",
                IsBuiltIn = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        _context.SystemRoles.AddRange(systemRoles);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} system roles", systemRoles.Count);
    }

    private async Task SeedSystemRolePermissionsAsync(CancellationToken cancellationToken)
    {
        if (await _context.SystemRolePermissions.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("System role-permission assignments already seeded");
            return;
        }

        var now = DateTime.UtcNow;
        var rolePermissions = new List<SystemRolePermission>();

        // Super Admin gets ALL permissions
        var allPermissionIds = GetAllPermissionIds();
        foreach (var permissionId in allPermissionIds)
        {
            rolePermissions.Add(new SystemRolePermission
            {
                Id = Guid.NewGuid(),
                SystemRoleId = SeedIds.SystemRoles.SuperAdmin,
                PermissionId = permissionId,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // User Admin gets all User Management + Role Management + Function View
        var userAdminPermissionIds = new[]
        {
            // All User Management
            SeedIds.Permissions.UsersView, SeedIds.Permissions.UsersCreate, SeedIds.Permissions.UsersUpdate,
            SeedIds.Permissions.UsersDelete, SeedIds.Permissions.UsersRolesManage, SeedIds.Permissions.UsersRolesView,
            SeedIds.Permissions.UsersInvite, SeedIds.Permissions.UsersDeactivate, SeedIds.Permissions.UsersReactivate,
            SeedIds.Permissions.UsersActivityView,
            // All Role Management
            SeedIds.Permissions.RolesView, SeedIds.Permissions.RolesCreate, SeedIds.Permissions.RolesUpdate,
            SeedIds.Permissions.RolesDelete, SeedIds.Permissions.RolesFunctionsAssign,
            // Functions view only
            SeedIds.Permissions.FunctionsView
        };

        foreach (var permissionId in userAdminPermissionIds)
        {
            rolePermissions.Add(new SystemRolePermission
            {
                Id = Guid.NewGuid(),
                SystemRoleId = SeedIds.SystemRoles.UserAdmin,
                PermissionId = permissionId,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Org Admin gets all Organization Management + view permissions
        var orgAdminPermissionIds = new[]
        {
            // All Organization Management
            SeedIds.Permissions.OrgView, SeedIds.Permissions.OrgUpdate, SeedIds.Permissions.OrgSettingsManage,
            SeedIds.Permissions.OrgSettingsView, SeedIds.Permissions.OrgBillingManage, SeedIds.Permissions.OrgBillingView,
            SeedIds.Permissions.OrgIntegrationsManage, SeedIds.Permissions.OrgIntegrationsView,
            SeedIds.Permissions.OrgApiKeysManage, SeedIds.Permissions.OrgApiKeysView,
            SeedIds.Permissions.OrgWebhooksManage, SeedIds.Permissions.OrgWebhooksView,
            SeedIds.Permissions.OrgAuditLogView, SeedIds.Permissions.OrgDataExport,
            // View users and roles
            SeedIds.Permissions.UsersView, SeedIds.Permissions.UsersRolesView,
            SeedIds.Permissions.RolesView, SeedIds.Permissions.FunctionsView
        };

        foreach (var permissionId in orgAdminPermissionIds)
        {
            rolePermissions.Add(new SystemRolePermission
            {
                Id = Guid.NewGuid(),
                SystemRoleId = SeedIds.SystemRoles.OrgAdmin,
                PermissionId = permissionId,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Viewer gets only view permissions
        var viewerPermissionIds = new[]
        {
            SeedIds.Permissions.UsersView, SeedIds.Permissions.UsersRolesView,
            SeedIds.Permissions.OrgView, SeedIds.Permissions.OrgSettingsView,
            SeedIds.Permissions.OrgBillingView, SeedIds.Permissions.OrgIntegrationsView,
            SeedIds.Permissions.OrgApiKeysView, SeedIds.Permissions.OrgWebhooksView,
            SeedIds.Permissions.RolesView, SeedIds.Permissions.FunctionsView
        };

        foreach (var permissionId in viewerPermissionIds)
        {
            rolePermissions.Add(new SystemRolePermission
            {
                Id = Guid.NewGuid(),
                SystemRoleId = SeedIds.SystemRoles.Viewer,
                PermissionId = permissionId,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        _context.SystemRolePermissions.AddRange(rolePermissions);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} system role-permission assignments", rolePermissions.Count);
    }

    private static Guid[] GetAllPermissionIds()
    {
        return new[]
        {
            // User Management
            SeedIds.Permissions.UsersView, SeedIds.Permissions.UsersCreate, SeedIds.Permissions.UsersUpdate,
            SeedIds.Permissions.UsersDelete, SeedIds.Permissions.UsersRolesManage, SeedIds.Permissions.UsersRolesView,
            SeedIds.Permissions.UsersInvite, SeedIds.Permissions.UsersDeactivate, SeedIds.Permissions.UsersReactivate,
            SeedIds.Permissions.UsersActivityView,
            // Organization Management
            SeedIds.Permissions.OrgView, SeedIds.Permissions.OrgUpdate, SeedIds.Permissions.OrgSettingsManage,
            SeedIds.Permissions.OrgSettingsView, SeedIds.Permissions.OrgBillingManage, SeedIds.Permissions.OrgBillingView,
            SeedIds.Permissions.OrgIntegrationsManage, SeedIds.Permissions.OrgIntegrationsView,
            SeedIds.Permissions.OrgApiKeysManage, SeedIds.Permissions.OrgApiKeysView,
            SeedIds.Permissions.OrgWebhooksManage, SeedIds.Permissions.OrgWebhooksView,
            SeedIds.Permissions.OrgAuditLogView, SeedIds.Permissions.OrgDataExport,
            // Role Management
            SeedIds.Permissions.RolesView, SeedIds.Permissions.RolesCreate, SeedIds.Permissions.RolesUpdate,
            SeedIds.Permissions.RolesDelete, SeedIds.Permissions.RolesFunctionsAssign,
            // Function Management
            SeedIds.Permissions.FunctionsView, SeedIds.Permissions.FunctionsCreate, SeedIds.Permissions.FunctionsUpdate,
            SeedIds.Permissions.FunctionsDelete
        };
    }

    private async Task SeedUserSystemRolesAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.UserSystemRoles.IgnoreQueryFilters().AnyAsync(ur => ur.Id == SeedIds.UserSystemRoles.RodrigoSuperAdmin, cancellationToken))
        {
            _logger.LogDebug("User system roles already seeded");
            return;
        }

        var userSystemRole = new UserSystemRole
        {
            Id = SeedIds.UserSystemRoles.RodrigoSuperAdmin,
            UserId = SeedIds.Users.Rodrigo,
            SystemRoleId = SeedIds.SystemRoles.SuperAdmin,
            OrganizationId = SeedIds.Organizations.Rugertek,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserSystemRoles.Add(userSystemRole);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded Super Admin system role for user: rodrigo@rugertek.com");
    }

    #endregion

    #region Operations Data Seeding

    private async Task SeedResourceSubtypesAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.ResourceSubtypes.IgnoreQueryFilters().AnyAsync(r => r.Id == SeedIds.ResourceSubtypes.Employee, cancellationToken))
        {
            _logger.LogDebug("Resource subtypes already seeded");
            return;
        }

        var now = DateTime.UtcNow;
        var orgId = SeedIds.Organizations.Rugertek;

        var subtypes = new List<ResourceSubtype>
        {
            // Person subtypes
            new() { Id = SeedIds.ResourceSubtypes.Employee, Name = "Employee", Description = "Full-time or part-time employee", ResourceType = ResourceType.Person, Icon = "mdi-account", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.ResourceSubtypes.Contractor, Name = "Contractor", Description = "External contractor", ResourceType = ResourceType.Person, Icon = "mdi-account-outline", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },

            // Team subtypes
            new() { Id = SeedIds.ResourceSubtypes.Department, Name = "Department", Description = "Organizational department", ResourceType = ResourceType.Team, Icon = "mdi-account-group", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.ResourceSubtypes.Squad, Name = "Squad", Description = "Cross-functional team", ResourceType = ResourceType.Team, Icon = "mdi-account-multiple", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },

            // Tool subtypes
            new() { Id = SeedIds.ResourceSubtypes.Software, Name = "Software", Description = "Software application", ResourceType = ResourceType.Tool, Icon = "mdi-application", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.ResourceSubtypes.Hardware, Name = "Hardware", Description = "Physical equipment", ResourceType = ResourceType.Tool, Icon = "mdi-laptop", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },

            // Automation subtypes
            new() { Id = SeedIds.ResourceSubtypes.Script, Name = "Script", Description = "Automated script", ResourceType = ResourceType.Automation, Icon = "mdi-script", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.ResourceSubtypes.Workflow, Name = "Workflow", Description = "Automated workflow", ResourceType = ResourceType.Automation, Icon = "mdi-sitemap", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },

            // Partner subtypes
            new() { Id = SeedIds.ResourceSubtypes.Vendor, Name = "Vendor", Description = "External vendor", ResourceType = ResourceType.Partner, Icon = "mdi-store", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.ResourceSubtypes.Consultant, Name = "Consultant", Description = "External consultant", ResourceType = ResourceType.Partner, Icon = "mdi-briefcase", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now }
        };

        _context.ResourceSubtypes.AddRange(subtypes);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} resource subtypes", subtypes.Count);
    }

    private async Task SeedResourcesAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.Resources.IgnoreQueryFilters().AnyAsync(r => r.Id == SeedIds.Resources.RodrigoResource, cancellationToken))
        {
            _logger.LogDebug("Resources already seeded");
            return;
        }

        var now = DateTime.UtcNow;
        var orgId = SeedIds.Organizations.Rugertek;

        var resources = new List<Resource>
        {
            // Person resource linked to user
            new() { Id = SeedIds.Resources.RodrigoResource, Name = "Rodrigo Campos Cervera", Description = "Founder & CEO", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.ResourceSubtypes.Employee, LinkedUserId = SeedIds.Users.Rodrigo, CreatedAt = now, UpdatedAt = now },

            // Tool resources
            new() { Id = SeedIds.Resources.Slack, Name = "Slack", Description = "Team communication platform", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.ResourceSubtypes.Software, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Resources.GitHub, Name = "GitHub", Description = "Code repository and collaboration", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.ResourceSubtypes.Software, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.Resources.AzureDevOps, Name = "Azure DevOps", Description = "CI/CD and project management", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.ResourceSubtypes.Software, CreatedAt = now, UpdatedAt = now }
        };

        _context.Resources.AddRange(resources);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} resources", resources.Count);
    }

    private async Task SeedProcessesAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.Processes.IgnoreQueryFilters().AnyAsync(p => p.Id == SeedIds.Processes.CustomerOnboarding, cancellationToken))
        {
            _logger.LogDebug("Processes already seeded");
            return;
        }

        var now = DateTime.UtcNow;
        var orgId = SeedIds.Organizations.Rugertek;

        // Customer Onboarding Process with activities
        var onboardingProcess = new Process
        {
            Id = SeedIds.Processes.CustomerOnboarding,
            Name = "Customer Onboarding",
            Purpose = "Ensure new customers are successfully onboarded and activated",
            Description = "End-to-end process for welcoming and setting up new customers",
            Trigger = "New customer signs contract",
            Output = "Activated customer with completed setup",
            Frequency = ProcessFrequency.OnDemand,
            Status = ProcessStatus.Active,
            StateType = ProcessStateType.Current,
            OrganizationId = orgId,
            OwnerId = SeedIds.Resources.RodrigoResource,
            CreatedAt = now,
            UpdatedAt = now,
            Activities = new List<Activity>
            {
                new() { Name = "Welcome Email", Description = "Send personalized welcome email", Order = 0, ActivityType = ActivityType.Automated, EstimatedDurationMinutes = 5, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Schedule Kickoff Call", Description = "Schedule initial kickoff meeting", Order = 1, ActivityType = ActivityType.Manual, EstimatedDurationMinutes = 10, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Kickoff Meeting", Description = "Conduct kickoff call with customer", Order = 2, ActivityType = ActivityType.Manual, EstimatedDurationMinutes = 60, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Setup Account", Description = "Create and configure customer account", Order = 3, ActivityType = ActivityType.Hybrid, EstimatedDurationMinutes = 30, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Training Session", Description = "Provide product training", Order = 4, ActivityType = ActivityType.Manual, EstimatedDurationMinutes = 90, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Go-Live Approval", Description = "Get customer approval for go-live", Order = 5, ActivityType = ActivityType.Decision, EstimatedDurationMinutes = 15, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Handoff to Success", Description = "Transfer to customer success team", Order = 6, ActivityType = ActivityType.Handoff, EstimatedDurationMinutes = 15, CreatedAt = now, UpdatedAt = now }
            }
        };

        // Incident Response Process
        var incidentProcess = new Process
        {
            Id = SeedIds.Processes.IncidentResponse,
            Name = "Incident Response",
            Purpose = "Quickly identify, respond to, and resolve production incidents",
            Description = "Standard operating procedure for handling production incidents",
            Trigger = "Alert triggered or incident reported",
            Output = "Resolved incident with documented root cause",
            Frequency = ProcessFrequency.OnDemand,
            Status = ProcessStatus.Active,
            StateType = ProcessStateType.Current,
            OrganizationId = orgId,
            OwnerId = SeedIds.Resources.RodrigoResource,
            CreatedAt = now,
            UpdatedAt = now,
            Activities = new List<Activity>
            {
                new() { Name = "Acknowledge Alert", Description = "Acknowledge the incident alert", Order = 0, ActivityType = ActivityType.Manual, EstimatedDurationMinutes = 5, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Assess Severity", Description = "Determine incident severity level", Order = 1, ActivityType = ActivityType.Decision, EstimatedDurationMinutes = 10, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Notify Stakeholders", Description = "Send notifications to relevant stakeholders", Order = 2, ActivityType = ActivityType.Automated, EstimatedDurationMinutes = 5, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Investigate", Description = "Investigate root cause", Order = 3, ActivityType = ActivityType.Manual, EstimatedDurationMinutes = 60, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Implement Fix", Description = "Apply fix or workaround", Order = 4, ActivityType = ActivityType.Manual, EstimatedDurationMinutes = 30, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Verify Resolution", Description = "Confirm incident is resolved", Order = 5, ActivityType = ActivityType.Decision, EstimatedDurationMinutes = 15, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Post-Mortem", Description = "Document lessons learned", Order = 6, ActivityType = ActivityType.Manual, EstimatedDurationMinutes = 60, CreatedAt = now, UpdatedAt = now }
            }
        };

        // Sprint Planning Process
        var sprintProcess = new Process
        {
            Id = SeedIds.Processes.SprintPlanning,
            Name = "Sprint Planning",
            Purpose = "Plan and prioritize work for the upcoming sprint",
            Trigger = "Sprint start date",
            Output = "Committed sprint backlog",
            Frequency = ProcessFrequency.Weekly,
            Status = ProcessStatus.Active,
            StateType = ProcessStateType.Current,
            OrganizationId = orgId,
            OwnerId = SeedIds.Resources.RodrigoResource,
            CreatedAt = now,
            UpdatedAt = now,
            Activities = new List<Activity>
            {
                new() { Name = "Review Backlog", Description = "Review and groom product backlog", Order = 0, ActivityType = ActivityType.Manual, EstimatedDurationMinutes = 30, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Capacity Planning", Description = "Determine team capacity", Order = 1, ActivityType = ActivityType.Manual, EstimatedDurationMinutes = 15, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Story Selection", Description = "Select stories for sprint", Order = 2, ActivityType = ActivityType.Decision, EstimatedDurationMinutes = 45, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Task Breakdown", Description = "Break stories into tasks", Order = 3, ActivityType = ActivityType.Manual, EstimatedDurationMinutes = 30, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Commitment", Description = "Team commits to sprint goals", Order = 4, ActivityType = ActivityType.Decision, EstimatedDurationMinutes = 10, CreatedAt = now, UpdatedAt = now }
            }
        };

        _context.Processes.AddRange(onboardingProcess, incidentProcess, sprintProcess);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded 3 processes with activities");
    }

    private async Task SeedCanvasesAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.Canvases.IgnoreQueryFilters().AnyAsync(c => c.Id == SeedIds.Canvases.MainBusinessCanvas, cancellationToken))
        {
            _logger.LogDebug("Canvases already seeded");
            return;
        }

        var now = DateTime.UtcNow;
        var orgId = SeedIds.Organizations.Rugertek;

        var canvas = new Canvas
        {
            Id = SeedIds.Canvases.MainBusinessCanvas,
            Name = "OrbitOS Business Model",
            Description = "Business Model Canvas for OrbitOS platform",
            CanvasType = CanvasType.BusinessModel,
            ScopeType = CanvasScopeType.Organization,
            Status = CanvasStatus.Active,
            OrganizationId = orgId,
            CreatedAt = now,
            UpdatedAt = now,
            Blocks = new List<CanvasBlock>
            {
                new() { BlockType = CanvasBlockType.KeyPartners, Content = "[\"Cloud providers (Azure, AWS)\", \"AI/ML providers (OpenAI, Anthropic)\", \"Integration partners\"]", DisplayOrder = 0, CreatedAt = now, UpdatedAt = now },
                new() { BlockType = CanvasBlockType.KeyActivities, Content = "[\"Platform development\", \"AI model integration\", \"Customer success\", \"Security & compliance\"]", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
                new() { BlockType = CanvasBlockType.KeyResources, Content = "[\"Engineering team\", \"AI/ML expertise\", \"Cloud infrastructure\", \"Customer data\"]", DisplayOrder = 2, CreatedAt = now, UpdatedAt = now },
                new() { BlockType = CanvasBlockType.ValuePropositions, Content = "[\"AI-native business operations\", \"Unified business intelligence\", \"Process automation\", \"Strategic planning tools\"]", DisplayOrder = 3, CreatedAt = now, UpdatedAt = now },
                new() { BlockType = CanvasBlockType.CustomerRelationships, Content = "[\"Self-service platform\", \"Dedicated success managers\", \"Community forums\", \"24/7 support\"]", DisplayOrder = 4, CreatedAt = now, UpdatedAt = now },
                new() { BlockType = CanvasBlockType.Channels, Content = "[\"Direct sales\", \"Website\", \"Partner channel\", \"App marketplace\"]", DisplayOrder = 5, CreatedAt = now, UpdatedAt = now },
                new() { BlockType = CanvasBlockType.CustomerSegments, Content = "[\"SMBs\", \"Mid-market\", \"Enterprise\", \"Startups\"]", DisplayOrder = 6, CreatedAt = now, UpdatedAt = now },
                new() { BlockType = CanvasBlockType.CostStructure, Content = "[\"Engineering salaries\", \"Cloud infrastructure\", \"AI API costs\", \"Marketing\", \"Support\"]", DisplayOrder = 7, CreatedAt = now, UpdatedAt = now },
                new() { BlockType = CanvasBlockType.RevenueStreams, Content = "[\"SaaS subscriptions\", \"Enterprise licenses\", \"Professional services\", \"API usage fees\"]", DisplayOrder = 8, CreatedAt = now, UpdatedAt = now }
            }
        };

        _context.Canvases.Add(canvas);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded business model canvas");
    }

    private async Task SeedGoalsAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.Goals.IgnoreQueryFilters().AnyAsync(g => g.Id == SeedIds.Goals.Q1Objective, cancellationToken))
        {
            _logger.LogDebug("Goals already seeded");
            return;
        }

        var now = DateTime.UtcNow;
        var orgId = SeedIds.Organizations.Rugertek;
        var q1Start = new DateTime(now.Year, 1, 1);
        var q1End = new DateTime(now.Year, 3, 31);

        // Q1 Objective
        var objective = new Goal
        {
            Id = SeedIds.Goals.Q1Objective,
            Name = "Launch MVP and acquire first 10 customers",
            Description = "Successfully launch the OrbitOS MVP and onboard our first paying customers",
            GoalType = GoalType.Objective,
            Status = GoalStatus.Active,
            TimeframeStart = q1Start,
            TimeframeEnd = q1End,
            OrganizationId = orgId,
            OwnerId = SeedIds.Resources.RodrigoResource,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Key Results
        var keyResults = new List<Goal>
        {
            new()
            {
                Id = SeedIds.Goals.RevenueKeyResult,
                Name = "Achieve $50K in ARR",
                Description = "Generate $50,000 in annual recurring revenue",
                GoalType = GoalType.KeyResult,
                Status = GoalStatus.Active,
                TimeframeStart = q1Start,
                TimeframeEnd = q1End,
                TargetValue = 50000,
                CurrentValue = 12500,
                Unit = "$",
                OrganizationId = orgId,
                ParentId = SeedIds.Goals.Q1Objective,
                OwnerId = SeedIds.Resources.RodrigoResource,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.Goals.CustomerKeyResult,
                Name = "Onboard 10 paying customers",
                Description = "Successfully onboard and activate 10 paying customers",
                GoalType = GoalType.KeyResult,
                Status = GoalStatus.Active,
                TimeframeStart = q1Start,
                TimeframeEnd = q1End,
                TargetValue = 10,
                CurrentValue = 3,
                Unit = "customers",
                OrganizationId = orgId,
                ParentId = SeedIds.Goals.Q1Objective,
                OwnerId = SeedIds.Resources.RodrigoResource,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.Goals.ProductKeyResult,
                Name = "Ship core features",
                Description = "Complete and ship all MVP core features",
                GoalType = GoalType.KeyResult,
                Status = GoalStatus.Active,
                TimeframeStart = q1Start,
                TimeframeEnd = q1End,
                TargetValue = 100,
                CurrentValue = 65,
                Unit = "%",
                OrganizationId = orgId,
                ParentId = SeedIds.Goals.Q1Objective,
                OwnerId = SeedIds.Resources.RodrigoResource,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        _context.Goals.Add(objective);
        _context.Goals.AddRange(keyResults);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded 1 objective with 3 key results");
    }

    #endregion

    #region Sample Operational Data (Development Only)

    /// <summary>
    /// Seeds sample operational functions and roles for development/testing purposes.
    /// Only runs in Development environment.
    /// </summary>
    private async Task SeedSampleOperationalDataAsync(CancellationToken cancellationToken)
    {
        await SeedSampleOperationalRolesAsync(cancellationToken);
        await SeedSampleOperationalFunctionsAsync(cancellationToken);
    }

    private async Task SeedSampleOperationalRolesAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.Roles.IgnoreQueryFilters().AnyAsync(r => r.Id == SeedIds.OperationalRoles.SalesManager, cancellationToken))
        {
            _logger.LogDebug("Sample operational roles already seeded");
            return;
        }

        var now = DateTime.UtcNow;
        var orgId = SeedIds.Organizations.Rugertek;

        var roles = new List<Role>
        {
            new()
            {
                Id = SeedIds.OperationalRoles.SalesManager,
                Name = "Sales Manager",
                Description = "Manages sales team, pipeline, and revenue targets",
                Purpose = "Drive revenue growth and manage sales operations",
                Department = "Sales",
                OrganizationId = orgId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.OperationalRoles.AccountExecutive,
                Name = "Account Executive",
                Description = "Manages client relationships and closes deals",
                Purpose = "Build relationships and drive sales",
                Department = "Sales",
                OrganizationId = orgId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.OperationalRoles.FinanceAnalyst,
                Name = "Finance Analyst",
                Description = "Handles financial analysis and reporting",
                Purpose = "Provide financial insights and manage accounting",
                Department = "Finance",
                OrganizationId = orgId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.OperationalRoles.SupportEngineer,
                Name = "Support Engineer",
                Description = "Provides technical support to customers",
                Purpose = "Resolve customer issues and ensure satisfaction",
                Department = "Support",
                OrganizationId = orgId,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        _context.Roles.AddRange(roles);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} sample operational roles (Development)", roles.Count);
    }

    private async Task SeedSampleOperationalFunctionsAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records
        if (await _context.Functions.IgnoreQueryFilters().AnyAsync(f => f.Id == SeedIds.OperationalFunctions.CurateCrm, cancellationToken))
        {
            _logger.LogDebug("Sample operational functions already seeded");
            return;
        }

        var now = DateTime.UtcNow;
        var orgId = SeedIds.Organizations.Rugertek;

        var functions = new List<Function>
        {
            new()
            {
                Id = SeedIds.OperationalFunctions.CurateCrm,
                Name = "Curate CRM",
                Description = "Maintain and update customer relationship data in the CRM system",
                Purpose = "Keep customer data accurate and up-to-date",
                Category = "Sales",
                Complexity = FunctionComplexity.Moderate,
                RequiresApproval = false,
                EstimatedDurationMinutes = 30,
                Instructions = "## CRM Curation Process\n\n1. Review new leads and contacts\n2. Update contact information\n3. Log recent interactions\n4. Update opportunity stages\n5. Clean up duplicate records",
                Status = FunctionStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.OperationalFunctions.ProcessInvoice,
                Name = "Process Invoice",
                Description = "Review, validate, and process incoming invoices for payment",
                Purpose = "Ensure accurate and timely vendor payments",
                Category = "Finance",
                Complexity = FunctionComplexity.Moderate,
                RequiresApproval = true,
                EstimatedDurationMinutes = 15,
                Instructions = "## Invoice Processing\n\n1. Verify invoice details match PO\n2. Check for correct pricing\n3. Validate tax calculations\n4. Route for approval if over threshold\n5. Schedule for payment",
                Status = FunctionStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.OperationalFunctions.HandleSupportTicket,
                Name = "Handle Support Ticket",
                Description = "Respond to and resolve customer support tickets",
                Purpose = "Provide excellent customer support and resolution",
                Category = "Support",
                Complexity = FunctionComplexity.Moderate,
                RequiresApproval = false,
                EstimatedDurationMinutes = 20,
                Instructions = "## Support Ticket Handling\n\n1. Review ticket details and history\n2. Reproduce the issue if applicable\n3. Research solution in knowledge base\n4. Respond to customer with solution or update\n5. Escalate if needed\n6. Close ticket with resolution notes",
                Status = FunctionStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.OperationalFunctions.ConductSalesCall,
                Name = "Conduct Sales Call",
                Description = "Lead discovery and qualification calls with prospects",
                Purpose = "Qualify leads and advance sales opportunities",
                Category = "Sales",
                Complexity = FunctionComplexity.Complex,
                RequiresApproval = false,
                EstimatedDurationMinutes = 45,
                Instructions = "## Sales Call Process\n\n1. Review prospect background before call\n2. Conduct discovery questions\n3. Present relevant solution overview\n4. Handle objections\n5. Establish next steps\n6. Log call notes in CRM",
                Status = FunctionStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.OperationalFunctions.SendFollowUps,
                Name = "Send Follow-ups",
                Description = "Send follow-up emails and communications to prospects and customers",
                Purpose = "Maintain engagement and move opportunities forward",
                Category = "Sales",
                Complexity = FunctionComplexity.Simple,
                RequiresApproval = false,
                EstimatedDurationMinutes = 10,
                Instructions = "## Follow-up Process\n\n1. Identify contacts needing follow-up\n2. Review previous communications\n3. Craft personalized message\n4. Schedule or send email\n5. Update CRM with activity",
                Status = FunctionStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = SeedIds.OperationalFunctions.PrepareReports,
                Name = "Prepare Reports",
                Description = "Generate and prepare business reports and dashboards",
                Purpose = "Provide data-driven insights for decision making",
                Category = "Finance",
                Complexity = FunctionComplexity.Moderate,
                RequiresApproval = false,
                EstimatedDurationMinutes = 60,
                Instructions = "## Report Preparation\n\n1. Gather data from relevant sources\n2. Validate data accuracy\n3. Run calculations and analysis\n4. Create visualizations\n5. Write executive summary\n6. Distribute to stakeholders",
                Status = FunctionStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        _context.Functions.AddRange(functions);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} sample operational functions (Development)", functions.Count);
    }

    #endregion

    #region Torus Organization Seeding

    /// <summary>
    /// Seeds the Torus demo organization with a complete org chart structure.
    /// This demonstrates the full hierarchy visualization capability.
    /// </summary>
    private async Task SeedTorusOrganizationAsync(CancellationToken cancellationToken)
    {
        // Use IgnoreQueryFilters to check including soft-deleted records - check by ID or by Slug
        if (await _context.Organizations.IgnoreQueryFilters().AnyAsync(o => o.Id == SeedIds.Organizations.Torus || o.Slug == "torus", cancellationToken))
        {
            _logger.LogDebug("Torus organization already seeded");
            return;
        }

        var now = DateTime.UtcNow;
        var orgId = SeedIds.Organizations.Torus;

        // Create Torus Organization
        var torusOrg = new Organization
        {
            Id = orgId,
            Name = "Torus",
            Slug = "torus",
            Description = "Torus Technologies - AI-Powered Business Solutions",
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Organizations.Add(torusOrg);

        // Add Rodrigo as member of Torus
        var torusMembership = new OrganizationMembership
        {
            Id = SeedIds.Memberships.RodrigoTorus,
            UserId = SeedIds.Users.Rodrigo,
            OrganizationId = orgId,
            Role = MembershipRole.Owner,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.OrganizationMemberships.Add(torusMembership);

        // Seed resource subtypes for Torus
        var subtypes = new List<ResourceSubtype>
        {
            new() { Id = SeedIds.TorusResourceSubtypes.Executive, Name = "Executive", Description = "C-level executive", ResourceType = ResourceType.Person, Icon = "mdi-account-star", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusResourceSubtypes.Employee, Name = "Employee", Description = "Full-time employee", ResourceType = ResourceType.Person, Icon = "mdi-account", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusResourceSubtypes.Contractor, Name = "Contractor", Description = "External contractor", ResourceType = ResourceType.Person, Icon = "mdi-account-outline", OrganizationId = orgId, CreatedAt = now, UpdatedAt = now },
        };
        _context.ResourceSubtypes.AddRange(subtypes);

        // Create resources (people) with reporting relationships
        var people = new List<Resource>
        {
            // Executive Team - CEO is root
            new() { Id = SeedIds.TorusPeople.CEO, Name = "Sarah Chen", Description = "Chief Executive Officer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Executive, ReportsToResourceId = null, CreatedAt = now, UpdatedAt = now },

            // C-Suite reporting to CEO
            new() { Id = SeedIds.TorusPeople.COO, Name = "Michael Torres", Description = "Chief Operating Officer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Executive, ReportsToResourceId = SeedIds.TorusPeople.CEO, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.CFO, Name = "Emily Watson", Description = "Chief Financial Officer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Executive, ReportsToResourceId = SeedIds.TorusPeople.CEO, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.CTO, Name = "David Park", Description = "Chief Technology Officer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Executive, ReportsToResourceId = SeedIds.TorusPeople.CEO, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.CMO, Name = "Lisa Johnson", Description = "Chief Marketing Officer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Executive, ReportsToResourceId = SeedIds.TorusPeople.CEO, CreatedAt = now, UpdatedAt = now },

            // Engineering Team (reports to CTO)
            new() { Id = SeedIds.TorusPeople.VPEngineering, Name = "James Wilson", Description = "VP of Engineering", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.CTO, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.EngineeringManager1, Name = "Anna Martinez", Description = "Engineering Manager - Platform", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.VPEngineering, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.EngineeringManager2, Name = "Kevin Brown", Description = "Engineering Manager - Product", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.VPEngineering, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.SeniorEngineer1, Name = "Rachel Kim", Description = "Senior Software Engineer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.EngineeringManager1, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.SeniorEngineer2, Name = "Chris Lee", Description = "Senior Software Engineer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.EngineeringManager1, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.SeniorEngineer3, Name = "Michelle Davis", Description = "Senior Software Engineer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.EngineeringManager2, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.Engineer1, Name = "Alex Thompson", Description = "Software Engineer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.EngineeringManager1, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.Engineer2, Name = "Jordan Rivera", Description = "Software Engineer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.EngineeringManager1, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.Engineer3, Name = "Taylor Swift", Description = "Software Engineer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.EngineeringManager2, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.Engineer4, Name = "Morgan Freeman", Description = "Software Engineer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.EngineeringManager2, CreatedAt = now, UpdatedAt = now },

            // Product Team (reports to COO)
            new() { Id = SeedIds.TorusPeople.VPProduct, Name = "Jennifer Adams", Description = "VP of Product", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.COO, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.ProductManager1, Name = "Daniel Garcia", Description = "Senior Product Manager", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.VPProduct, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.ProductManager2, Name = "Sophie Turner", Description = "Product Manager", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.VPProduct, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.ProductDesigner, Name = "Ryan Cooper", Description = "Lead Product Designer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.VPProduct, CreatedAt = now, UpdatedAt = now },

            // Sales Team (reports to COO)
            new() { Id = SeedIds.TorusPeople.VPSales, Name = "Robert Miller", Description = "VP of Sales", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.COO, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.SalesManager, Name = "Amanda White", Description = "Sales Manager", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.VPSales, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.AccountExec1, Name = "Brandon Lee", Description = "Account Executive", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.SalesManager, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.AccountExec2, Name = "Nicole Harris", Description = "Account Executive", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.SalesManager, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.SDR1, Name = "Jake Wilson", Description = "Sales Development Representative", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.SalesManager, CreatedAt = now, UpdatedAt = now },

            // Marketing Team (reports to CMO)
            new() { Id = SeedIds.TorusPeople.MarketingManager, Name = "Christina Moore", Description = "Marketing Manager", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.CMO, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.ContentWriter, Name = "Emma Stone", Description = "Senior Content Writer", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.MarketingManager, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.DigitalMarketer, Name = "Mark Taylor", Description = "Digital Marketing Specialist", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.MarketingManager, CreatedAt = now, UpdatedAt = now },

            // Finance Team (reports to CFO)
            new() { Id = SeedIds.TorusPeople.FinanceManager, Name = "Patricia Anderson", Description = "Finance Manager", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.CFO, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.Accountant, Name = "Steven Clark", Description = "Senior Accountant", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.FinanceManager, CreatedAt = now, UpdatedAt = now },

            // HR Team (reports to COO)
            new() { Id = SeedIds.TorusPeople.HRManager, Name = "Laura Martinez", Description = "HR Manager", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.COO, CreatedAt = now, UpdatedAt = now },
            new() { Id = SeedIds.TorusPeople.Recruiter, Name = "Brian Jackson", Description = "Technical Recruiter", Status = ResourceStatus.Active, OrganizationId = orgId, ResourceSubtypeId = SeedIds.TorusResourceSubtypes.Employee, ReportsToResourceId = SeedIds.TorusPeople.HRManager, CreatedAt = now, UpdatedAt = now },
        };

        _context.Resources.AddRange(people);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded Torus organization with {Count} people in org chart", people.Count);
    }

    #endregion
}
