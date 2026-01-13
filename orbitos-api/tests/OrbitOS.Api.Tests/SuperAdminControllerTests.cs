/**
 * =============================================================================
 * OrbitOS Super Admin Controller Integration Tests
 * =============================================================================
 * Comprehensive tests for all SuperAdmin CRUD operations.
 * Uses in-memory database for fast, isolated testing.
 *
 * Spec: F002-super-admin.json
 * Entities: ENT001-user, ENT002-organization, ENT003-role, ENT004-function
 * =============================================================================
 */

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;
using Xunit;

namespace OrbitOS.Api.Tests;

public class SuperAdminControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public SuperAdminControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task InitializeAsync()
    {
        // Seed test data
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrbitOSDbContext>();

        // Clear existing data
        db.Functions.RemoveRange(db.Functions);
        db.Roles.RemoveRange(db.Roles);
        db.OrganizationMemberships.RemoveRange(db.OrganizationMemberships);
        db.Organizations.RemoveRange(db.Organizations);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();

        // Seed test organization
        var testOrg = new Organization
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Test Organization",
            Slug = "test-org",
            CreatedAt = DateTime.UtcNow
        };
        db.Organizations.Add(testOrg);

        // Seed test user
        var testUser = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "$2a$11$K3xZz5.5xzWzV5xzWzV5xOtestpasswordhashfortesting",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(testUser);

        // Seed test role
        var testRole = new Role
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Test Role",
            Description = "A test role",
            OrganizationId = testOrg.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.Roles.Add(testRole);

        // Seed test function
        var testFunction = new Function
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name = "test.read",
            Description = "Test read permission",
            Category = "Testing",
            OrganizationId = testOrg.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.Functions.Add(testFunction);

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // =========================================================================
    // DASHBOARD TESTS
    // =========================================================================

    [Fact]
    public async Task Dashboard_GetStats_ReturnsAllCounts()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/dashboard");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        var stats = JsonSerializer.Deserialize<DashboardStatsDto>(content, _jsonOptions);

        stats.Should().NotBeNull();
        stats!.TotalUsers.Should().BeGreaterThanOrEqualTo(1);
        stats.TotalOrganizations.Should().BeGreaterThanOrEqualTo(1);
        stats.TotalRoles.Should().BeGreaterThanOrEqualTo(1);
        stats.TotalFunctions.Should().BeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // USERS CRUD TESTS
    // =========================================================================

    [Fact]
    public async Task Users_GetAll_ReturnsList()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/users");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(_jsonOptions);
        users.Should().NotBeNull();
        users!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Users_GetById_ReturnsUser()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/users/22222222-2222-2222-2222-222222222222");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var user = await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);
        user.Should().NotBeNull();
        user!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Users_GetById_NotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/users/00000000-0000-0000-0000-000000000000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Users_Create_ReturnsCreatedUser()
    {
        // Arrange
        var newUser = new
        {
            Email = $"new-user-{Guid.NewGuid()}@example.com",
            DisplayName = "New Test User",
            Password = "NewPass123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/SuperAdmin/users", newUser);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var createdUser = await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be(newUser.Email);
        createdUser.DisplayName.Should().Be(newUser.DisplayName);
        createdUser.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Users_Create_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var duplicateUser = new
        {
            Email = "test@example.com", // Already exists
            DisplayName = "Duplicate User",
            Password = "Pass123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/SuperAdmin/users", duplicateUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Users_Update_ReturnsUpdatedUser()
    {
        // Arrange
        var updateData = new
        {
            Email = "test@example.com",
            DisplayName = "Updated Test User"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/SuperAdmin/users/22222222-2222-2222-2222-222222222222", updateData);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);
        updatedUser.Should().NotBeNull();
        updatedUser!.DisplayName.Should().Be("Updated Test User");
    }

    [Fact]
    public async Task Users_ResetPassword_ReturnsSuccess()
    {
        // Arrange
        var resetData = new { NewPassword = "NewPassword123!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/SuperAdmin/users/22222222-2222-2222-2222-222222222222/reset-password", resetData);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Users_Delete_ReturnsNoContent()
    {
        // First create a user to delete
        var newUser = new
        {
            Email = $"delete-test-{Guid.NewGuid()}@example.com",
            DisplayName = "Delete Test User",
            Password = "Pass123!"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/SuperAdmin/users", newUser);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);

        // Act
        var response = await _client.DeleteAsync($"/api/SuperAdmin/users/{createdUser!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/SuperAdmin/users/{createdUser.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Users_Search_FiltersResults()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/users?search=test");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(_jsonOptions);
        users.Should().NotBeNull();
        users!.Should().OnlyContain(u =>
            u.Email!.Contains("test", StringComparison.OrdinalIgnoreCase) ||
            u.DisplayName!.Contains("test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Users_Pagination_ReturnsCorrectPage()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/users?skip=0&take=10");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(_jsonOptions);
        users.Should().NotBeNull();
        users!.Count.Should().BeLessThanOrEqualTo(10);
    }

    // =========================================================================
    // ORGANIZATIONS CRUD TESTS
    // =========================================================================

    [Fact]
    public async Task Organizations_GetAll_ReturnsList()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/organizations");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var orgs = await response.Content.ReadFromJsonAsync<List<OrganizationDto>>(_jsonOptions);
        orgs.Should().NotBeNull();
        orgs!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Organizations_GetById_ReturnsOrganization()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/organizations/11111111-1111-1111-1111-111111111111");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var org = await response.Content.ReadFromJsonAsync<OrganizationDto>(_jsonOptions);
        org.Should().NotBeNull();
        org!.Name.Should().Be("Test Organization");
    }

    [Fact]
    public async Task Organizations_Create_ReturnsCreatedOrganization()
    {
        // Arrange
        var slug = $"new-org-{Guid.NewGuid().ToString()[..8]}";
        var newOrg = new
        {
            Name = "New Test Organization",
            Slug = slug
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/SuperAdmin/organizations", newOrg);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var createdOrg = await response.Content.ReadFromJsonAsync<OrganizationDto>(_jsonOptions);
        createdOrg.Should().NotBeNull();
        createdOrg!.Name.Should().Be(newOrg.Name);
        createdOrg.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task Organizations_Create_DuplicateSlug_ReturnsBadRequest()
    {
        // Arrange
        var duplicateOrg = new
        {
            Name = "Duplicate Org",
            Slug = "test-org" // Already exists
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/SuperAdmin/organizations", duplicateOrg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Organizations_Update_ReturnsUpdatedOrganization()
    {
        // Arrange
        var updateData = new
        {
            Name = "Updated Test Organization",
            Slug = "test-org"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/SuperAdmin/organizations/11111111-1111-1111-1111-111111111111", updateData);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var updatedOrg = await response.Content.ReadFromJsonAsync<OrganizationDto>(_jsonOptions);
        updatedOrg.Should().NotBeNull();
        updatedOrg!.Name.Should().Be("Updated Test Organization");
    }

    [Fact]
    public async Task Organizations_Delete_ReturnsNoContent()
    {
        // First create an org to delete
        var newOrg = new
        {
            Name = "Delete Test Org",
            Slug = $"delete-test-{Guid.NewGuid().ToString()[..8]}"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/SuperAdmin/organizations", newOrg);
        var createdOrg = await createResponse.Content.ReadFromJsonAsync<OrganizationDto>(_jsonOptions);

        // Act
        var response = await _client.DeleteAsync($"/api/SuperAdmin/organizations/{createdOrg!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // =========================================================================
    // ROLES CRUD TESTS
    // =========================================================================

    [Fact]
    public async Task Roles_GetAll_ReturnsList()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/roles");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>(_jsonOptions);
        roles.Should().NotBeNull();
        roles!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Roles_FilterByOrganization_ReturnsFilteredList()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/roles?organizationId=11111111-1111-1111-1111-111111111111");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>(_jsonOptions);
        roles.Should().NotBeNull();
        roles!.Should().OnlyContain(r => r.OrganizationId == Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task Roles_Create_ReturnsCreatedRole()
    {
        // Arrange
        var newRole = new
        {
            Name = $"New Test Role {Guid.NewGuid().ToString()[..8]}",
            Description = "A new test role",
            OrganizationId = "11111111-1111-1111-1111-111111111111"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/SuperAdmin/roles", newRole);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var createdRole = await response.Content.ReadFromJsonAsync<RoleDto>(_jsonOptions);
        createdRole.Should().NotBeNull();
        createdRole!.Name.Should().Be(newRole.Name);
    }

    [Fact]
    public async Task Roles_Update_ReturnsUpdatedRole()
    {
        // Arrange
        var updateData = new
        {
            Name = "Updated Test Role",
            Description = "Updated description",
            OrganizationId = "11111111-1111-1111-1111-111111111111"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/SuperAdmin/roles/33333333-3333-3333-3333-333333333333", updateData);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var updatedRole = await response.Content.ReadFromJsonAsync<RoleDto>(_jsonOptions);
        updatedRole.Should().NotBeNull();
        updatedRole!.Name.Should().Be("Updated Test Role");
    }

    [Fact]
    public async Task Roles_Delete_ReturnsNoContent()
    {
        // First create a role to delete
        var newRole = new
        {
            Name = $"Delete Test Role {Guid.NewGuid().ToString()[..8]}",
            Description = "To be deleted",
            OrganizationId = "11111111-1111-1111-1111-111111111111"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/SuperAdmin/roles", newRole);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>(_jsonOptions);

        // Act
        var response = await _client.DeleteAsync($"/api/SuperAdmin/roles/{createdRole!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // =========================================================================
    // FUNCTIONS CRUD TESTS
    // =========================================================================

    [Fact]
    public async Task Functions_GetAll_ReturnsList()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/functions");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var functions = await response.Content.ReadFromJsonAsync<List<FunctionDto>>(_jsonOptions);
        functions.Should().NotBeNull();
        functions!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Functions_FilterByOrganization_ReturnsFilteredList()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/functions?organizationId=11111111-1111-1111-1111-111111111111");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var functions = await response.Content.ReadFromJsonAsync<List<FunctionDto>>(_jsonOptions);
        functions.Should().NotBeNull();
        functions!.Should().OnlyContain(f => f.OrganizationId == Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task Functions_Create_ReturnsCreatedFunction()
    {
        // Arrange
        var newFunction = new
        {
            Name = $"test.function.{Guid.NewGuid().ToString()[..8]}",
            Description = "A new test function",
            Category = "Testing",
            OrganizationId = "11111111-1111-1111-1111-111111111111"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/SuperAdmin/functions", newFunction);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var createdFunction = await response.Content.ReadFromJsonAsync<FunctionDto>(_jsonOptions);
        createdFunction.Should().NotBeNull();
        createdFunction!.Name.Should().Be(newFunction.Name);
        createdFunction.Category.Should().Be("Testing");
    }

    [Fact]
    public async Task Functions_Update_ReturnsUpdatedFunction()
    {
        // Arrange
        var updateData = new
        {
            Name = "test.updated",
            Description = "Updated description",
            Category = "Updated",
            OrganizationId = "11111111-1111-1111-1111-111111111111"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/SuperAdmin/functions/44444444-4444-4444-4444-444444444444", updateData);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var updatedFunction = await response.Content.ReadFromJsonAsync<FunctionDto>(_jsonOptions);
        updatedFunction.Should().NotBeNull();
        updatedFunction!.Name.Should().Be("test.updated");
    }

    [Fact]
    public async Task Functions_Delete_ReturnsNoContent()
    {
        // First create a function to delete
        var newFunction = new
        {
            Name = $"test.delete.{Guid.NewGuid().ToString()[..8]}",
            Description = "To be deleted",
            Category = "Testing",
            OrganizationId = "11111111-1111-1111-1111-111111111111"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/SuperAdmin/functions", newFunction);
        var createdFunction = await createResponse.Content.ReadFromJsonAsync<FunctionDto>(_jsonOptions);

        // Act
        var response = await _client.DeleteAsync($"/api/SuperAdmin/functions/{createdFunction!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Functions_Search_FiltersResults()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/functions?search=test");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var functions = await response.Content.ReadFromJsonAsync<List<FunctionDto>>(_jsonOptions);
        functions.Should().NotBeNull();
    }

    // =========================================================================
    // COUNT ENDPOINT TESTS
    // =========================================================================

    [Fact]
    public async Task Users_Count_ReturnsNumber()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/users/count");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var count = await response.Content.ReadFromJsonAsync<int>();
        count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Organizations_Count_ReturnsNumber()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/organizations/count");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var count = await response.Content.ReadFromJsonAsync<int>();
        count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Roles_Count_ReturnsNumber()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/roles/count");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var count = await response.Content.ReadFromJsonAsync<int>();
        count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Functions_Count_ReturnsNumber()
    {
        // Act
        var response = await _client.GetAsync("/api/SuperAdmin/functions/count");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var count = await response.Content.ReadFromJsonAsync<int>();
        count.Should().BeGreaterThanOrEqualTo(1);
    }
}

// DTOs for deserialization
public record DashboardStatsDto
{
    public int TotalUsers { get; init; }
    public int TotalOrganizations { get; init; }
    public int TotalRoles { get; init; }
    public int TotalFunctions { get; init; }
    public List<ActivityDto>? RecentActivity { get; init; }
}

public record ActivityDto
{
    public string? Action { get; init; }
    public string? Details { get; init; }
    public DateTime Timestamp { get; init; }
}

public record UserDto
{
    public Guid Id { get; init; }
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public List<string>? AuthMethods { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record OrganizationDto
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Slug { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record RoleDto
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public Guid OrganizationId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record FunctionDto
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public Guid OrganizationId { get; init; }
    public DateTime CreatedAt { get; init; }
}
