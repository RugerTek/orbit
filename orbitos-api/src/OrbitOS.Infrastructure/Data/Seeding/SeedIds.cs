namespace OrbitOS.Infrastructure.Data.Seeding;

/// <summary>
/// Deterministic GUIDs for seed data to ensure consistency across migrations.
/// Generated using GUIDs derived from meaningful strings for traceability.
/// </summary>
public static class SeedIds
{
    public static class Organizations
    {
        /// <summary>Rugertek default organization</summary>
        public static readonly Guid Rugertek = new("11111111-1111-1111-1111-111111111111");
    }

    public static class Users
    {
        /// <summary>rodrigo@rugertek.com - Super Admin user</summary>
        public static readonly Guid Rodrigo = new("22222222-2222-2222-2222-222222222222");
    }

    /// <summary>
    /// System roles for platform access control (e.g., Super Admin, User Admin).
    /// These control what users can do in the platform itself.
    /// </summary>
    public static class SystemRoles
    {
        /// <summary>Super Administrator role</summary>
        public static readonly Guid SuperAdmin = new("33333333-3333-3333-3333-333333333301");

        /// <summary>User Administrator role</summary>
        public static readonly Guid UserAdmin = new("33333333-3333-3333-3333-333333333302");

        /// <summary>Organization Administrator role</summary>
        public static readonly Guid OrgAdmin = new("33333333-3333-3333-3333-333333333303");

        /// <summary>Viewer role</summary>
        public static readonly Guid Viewer = new("33333333-3333-3333-3333-333333333304");
    }

    /// <summary>
    /// Permissions for platform access control (e.g., users.view, org.settings.manage).
    /// These are the granular permissions that system roles grant.
    /// </summary>
    public static class Permissions
    {
        // User Management Permissions
        public static readonly Guid UsersView = new("44444444-4444-4444-4444-444444444401");
        public static readonly Guid UsersCreate = new("44444444-4444-4444-4444-444444444402");
        public static readonly Guid UsersUpdate = new("44444444-4444-4444-4444-444444444403");
        public static readonly Guid UsersDelete = new("44444444-4444-4444-4444-444444444404");
        public static readonly Guid UsersRolesManage = new("44444444-4444-4444-4444-444444444405");
        public static readonly Guid UsersRolesView = new("44444444-4444-4444-4444-444444444406");
        public static readonly Guid UsersInvite = new("44444444-4444-4444-4444-444444444407");
        public static readonly Guid UsersDeactivate = new("44444444-4444-4444-4444-444444444408");
        public static readonly Guid UsersReactivate = new("44444444-4444-4444-4444-444444444409");
        public static readonly Guid UsersActivityView = new("44444444-4444-4444-4444-444444444410");

        // Organization Management Permissions
        public static readonly Guid OrgView = new("44444444-4444-4444-4444-444444444501");
        public static readonly Guid OrgUpdate = new("44444444-4444-4444-4444-444444444502");
        public static readonly Guid OrgSettingsManage = new("44444444-4444-4444-4444-444444444503");
        public static readonly Guid OrgSettingsView = new("44444444-4444-4444-4444-444444444504");
        public static readonly Guid OrgBillingManage = new("44444444-4444-4444-4444-444444444505");
        public static readonly Guid OrgBillingView = new("44444444-4444-4444-4444-444444444506");
        public static readonly Guid OrgIntegrationsManage = new("44444444-4444-4444-4444-444444444507");
        public static readonly Guid OrgIntegrationsView = new("44444444-4444-4444-4444-444444444508");
        public static readonly Guid OrgApiKeysManage = new("44444444-4444-4444-4444-444444444509");
        public static readonly Guid OrgApiKeysView = new("44444444-4444-4444-4444-444444444510");
        public static readonly Guid OrgWebhooksManage = new("44444444-4444-4444-4444-444444444511");
        public static readonly Guid OrgWebhooksView = new("44444444-4444-4444-4444-444444444512");
        public static readonly Guid OrgAuditLogView = new("44444444-4444-4444-4444-444444444513");
        public static readonly Guid OrgDataExport = new("44444444-4444-4444-4444-444444444514");

        // Role Management Permissions
        public static readonly Guid RolesView = new("44444444-4444-4444-4444-444444444601");
        public static readonly Guid RolesCreate = new("44444444-4444-4444-4444-444444444602");
        public static readonly Guid RolesUpdate = new("44444444-4444-4444-4444-444444444603");
        public static readonly Guid RolesDelete = new("44444444-4444-4444-4444-444444444604");
        public static readonly Guid RolesFunctionsAssign = new("44444444-4444-4444-4444-444444444605");

        // Function Management Permissions
        public static readonly Guid FunctionsView = new("44444444-4444-4444-4444-444444444701");
        public static readonly Guid FunctionsCreate = new("44444444-4444-4444-4444-444444444702");
        public static readonly Guid FunctionsUpdate = new("44444444-4444-4444-4444-444444444703");
        public static readonly Guid FunctionsDelete = new("44444444-4444-4444-4444-444444444704");
    }

    public static class Memberships
    {
        /// <summary>Rodrigo's membership in Rugertek</summary>
        public static readonly Guid RodrigoRugertek = new("55555555-5555-5555-5555-555555555501");
    }

    /// <summary>
    /// User to SystemRole assignments for platform access.
    /// </summary>
    public static class UserSystemRoles
    {
        /// <summary>Rodrigo's SuperAdmin role assignment</summary>
        public static readonly Guid RodrigoSuperAdmin = new("66666666-6666-6666-6666-666666666601");
    }

    public static class ResourceSubtypes
    {
        // Person subtypes
        public static readonly Guid Employee = new("77777777-7777-7777-7777-777777777701");
        public static readonly Guid Contractor = new("77777777-7777-7777-7777-777777777702");

        // Team subtypes
        public static readonly Guid Department = new("77777777-7777-7777-7777-777777777710");
        public static readonly Guid Squad = new("77777777-7777-7777-7777-777777777711");

        // Tool subtypes
        public static readonly Guid Software = new("77777777-7777-7777-7777-777777777720");
        public static readonly Guid Hardware = new("77777777-7777-7777-7777-777777777721");

        // Automation subtypes
        public static readonly Guid Script = new("77777777-7777-7777-7777-777777777730");
        public static readonly Guid Workflow = new("77777777-7777-7777-7777-777777777731");

        // Partner subtypes
        public static readonly Guid Vendor = new("77777777-7777-7777-7777-777777777740");
        public static readonly Guid Consultant = new("77777777-7777-7777-7777-777777777741");
    }

    public static class Resources
    {
        // Sample people resources
        public static readonly Guid RodrigoResource = new("88888888-8888-8888-8888-888888888801");

        // Sample tools
        public static readonly Guid Slack = new("88888888-8888-8888-8888-888888888810");
        public static readonly Guid GitHub = new("88888888-8888-8888-8888-888888888811");
        public static readonly Guid AzureDevOps = new("88888888-8888-8888-8888-888888888812");
    }

    public static class Processes
    {
        public static readonly Guid CustomerOnboarding = new("99999999-9999-9999-9999-999999999901");
        public static readonly Guid IncidentResponse = new("99999999-9999-9999-9999-999999999902");
        public static readonly Guid SprintPlanning = new("99999999-9999-9999-9999-999999999903");
    }

    public static class Canvases
    {
        public static readonly Guid MainBusinessCanvas = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaa001");
    }

    public static class Goals
    {
        // Objective
        public static readonly Guid Q1Objective = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb001");

        // Key Results
        public static readonly Guid RevenueKeyResult = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb011");
        public static readonly Guid CustomerKeyResult = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb012");
        public static readonly Guid ProductKeyResult = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb013");
    }

    // ============================================
    // OPERATIONAL SEED DATA (Development only)
    // ============================================

    /// <summary>
    /// Operational roles for business structure (e.g., Sales Manager, Engineer).
    /// These represent job functions in the organization.
    /// </summary>
    public static class OperationalRoles
    {
        public static readonly Guid SalesManager = new("cccccccc-cccc-cccc-cccc-cccccccccc01");
        public static readonly Guid AccountExecutive = new("cccccccc-cccc-cccc-cccc-cccccccccc02");
        public static readonly Guid FinanceAnalyst = new("cccccccc-cccc-cccc-cccc-cccccccccc03");
        public static readonly Guid SupportEngineer = new("cccccccc-cccc-cccc-cccc-cccccccccc04");
    }

    /// <summary>
    /// Operational functions for business work units (e.g., "Curate CRM", "Process Invoice").
    /// These represent actual work that people do in the organization.
    /// </summary>
    public static class OperationalFunctions
    {
        public static readonly Guid CurateCrm = new("dddddddd-dddd-dddd-dddd-dddddddddd01");
        public static readonly Guid ProcessInvoice = new("dddddddd-dddd-dddd-dddd-dddddddddd02");
        public static readonly Guid HandleSupportTicket = new("dddddddd-dddd-dddd-dddd-dddddddddd03");
        public static readonly Guid ConductSalesCall = new("dddddddd-dddd-dddd-dddd-dddddddddd04");
        public static readonly Guid SendFollowUps = new("dddddddd-dddd-dddd-dddd-dddddddddd05");
        public static readonly Guid PrepareReports = new("dddddddd-dddd-dddd-dddd-dddddddddd06");
    }
}
