/**
 * =============================================================================
 * OrbitOS Operations Controller Integration Tests
 * =============================================================================
 * Tests for Operations CRUD operations including Resources (People).
 * Uses in-memory database for fast, isolated testing.
 * =============================================================================
 */

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;
using Xunit;

namespace OrbitOS.Api.Tests;

/// <summary>
/// Custom factory that bypasses authentication for Operations tests
/// </summary>
public class OperationsTestFactory : WebApplicationFactory<Program>
{
    public const string OrgId = "11111111-1111-1111-1111-111111111111";
    public const string EmployeeSubtypeId = "77777777-7777-7777-7777-777777777701";
    private static readonly string DbName = $"OperationsTestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "test-tenant-id",
                ["AzureAd:ClientId"] = "test-client-id",
                ["AzureAd:Audience"] = "api://test-client-id",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OrbitOSDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing - use same name for all tests in this factory
            services.AddDbContext<OrbitOSDbContext>(options =>
            {
                options.UseInMemoryDatabase(DbName);
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrbitOSDbContext>();

            // Ensure the database is created and seed initial data
            db.Database.EnsureCreated();
            SeedDatabase(db);

            // Add test authentication that always succeeds
            services.AddAuthentication(defaultScheme: "TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "TestScheme", options => { });
        });

        builder.UseEnvironment("Testing");
    }

    private static void SeedDatabase(OrbitOSDbContext db)
    {
        // Check if already seeded
        if (db.Organizations.Any(o => o.Id == Guid.Parse(OrgId)))
            return;

        // Seed test organization
        var testOrg = new Organization
        {
            Id = Guid.Parse(OrgId),
            Name = "Test Organization",
            Slug = "test-org",
            CreatedAt = DateTime.UtcNow
        };
        db.Organizations.Add(testOrg);

        // Seed Employee resource subtype
        var employeeSubtype = new ResourceSubtype
        {
            Id = Guid.Parse(EmployeeSubtypeId),
            Name = "Employee",
            Description = "Full-time or part-time employee",
            ResourceType = ResourceType.Person,
            Icon = "mdi-account",
            OrganizationId = Guid.Parse(OrgId),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ResourceSubtypes.Add(employeeSubtype);

        db.SaveChanges();
    }
}

/// <summary>
/// Test authentication handler that always authenticates successfully
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id"),
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class OperationsControllerTests : IClassFixture<OperationsTestFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string OrgId = OperationsTestFactory.OrgId;
    private const string EmployeeSubtypeId = OperationsTestFactory.EmployeeSubtypeId;

    public OperationsControllerTests(OperationsTestFactory factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // =========================================================================
    // RESOURCE SUBTYPES TESTS
    // =========================================================================

    [Fact]
    public async Task ResourceSubtypes_GetAll_ReturnsList()
    {
        // Act
        var response = await _client.GetAsync($"/api/organizations/{OrgId}/operations/resource-subtypes");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"Expected success but got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var subtypes = await response.Content.ReadFromJsonAsync<List<ResourceSubtypeDto>>(_jsonOptions);
        subtypes.Should().NotBeNull();
        subtypes!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // RESOURCES (PEOPLE) TESTS
    // =========================================================================

    [Fact]
    public async Task Resources_GetAll_ReturnsList()
    {
        // Act
        var response = await _client.GetAsync($"/api/organizations/{OrgId}/operations/resources");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"Expected success but got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task Resources_CreatePerson_ReturnsCreatedResource()
    {
        // Arrange
        var newPerson = new
        {
            Name = "Test Person",
            Description = "Senior Developer",
            ResourceSubtypeId = EmployeeSubtypeId,
            Status = 1 // Active
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/operations/resources", newPerson);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"Expected success but got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var createdResource = await response.Content.ReadFromJsonAsync<ResourceDto>(_jsonOptions);
        createdResource.Should().NotBeNull();
        createdResource!.Name.Should().Be("Test Person");
        createdResource.Description.Should().Be("Senior Developer");
        createdResource.ResourceType.Should().Be(0); // Person = 0
        createdResource.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Resources_CreatePerson_InvalidSubtype_ReturnsBadRequest()
    {
        // Arrange
        var newPerson = new
        {
            Name = "Test Person",
            ResourceSubtypeId = "00000000-0000-0000-0000-000000000000", // Invalid
            Status = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/operations/resources", newPerson);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Resources_CreateAndList_ShowsNewPerson()
    {
        // Arrange
        var personName = $"Test Person {Guid.NewGuid().ToString()[..8]}";
        var newPerson = new
        {
            Name = personName,
            Description = "Test Role",
            ResourceSubtypeId = EmployeeSubtypeId,
            Status = 1
        };

        // Act - Create
        var createResponse = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/operations/resources", newPerson);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        // Act - List
        var listResponse = await _client.GetAsync($"/api/organizations/{OrgId}/operations/resources?type=0");
        listResponse.IsSuccessStatusCode.Should().BeTrue();

        var resources = await listResponse.Content.ReadFromJsonAsync<List<ResourceDto>>(_jsonOptions);

        // Assert
        resources.Should().NotBeNull();
        resources!.Should().Contain(r => r.Name == personName);
    }

    [Fact]
    public async Task Resources_GetById_ReturnsResource()
    {
        // Arrange - Create a person first
        var newPerson = new
        {
            Name = "Get By Id Test Person",
            Description = "Test",
            ResourceSubtypeId = EmployeeSubtypeId,
            Status = 1
        };
        var createResponse = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/operations/resources", newPerson);
        var createdResource = await createResponse.Content.ReadFromJsonAsync<ResourceDto>(_jsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/organizations/{OrgId}/operations/resources/{createdResource!.Id}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var resource = await response.Content.ReadFromJsonAsync<ResourceDto>(_jsonOptions);
        resource.Should().NotBeNull();
        resource!.Name.Should().Be("Get By Id Test Person");
    }

    [Fact]
    public async Task Resources_Update_ReturnsUpdatedResource()
    {
        // Arrange - Create a person first
        var newPerson = new
        {
            Name = "Update Test Person",
            Description = "Original",
            ResourceSubtypeId = EmployeeSubtypeId,
            Status = 1
        };
        var createResponse = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/operations/resources", newPerson);
        var createdResource = await createResponse.Content.ReadFromJsonAsync<ResourceDto>(_jsonOptions);

        var updateData = new
        {
            Name = "Updated Person Name",
            Description = "Updated Description",
            Status = 1
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/organizations/{OrgId}/operations/resources/{createdResource!.Id}", updateData);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var updatedResource = await response.Content.ReadFromJsonAsync<ResourceDto>(_jsonOptions);
        updatedResource.Should().NotBeNull();
        updatedResource!.Name.Should().Be("Updated Person Name");
        updatedResource.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Resources_Delete_ReturnsNoContent()
    {
        // Arrange - Create a person first
        var newPerson = new
        {
            Name = "Delete Test Person",
            Description = "To be deleted",
            ResourceSubtypeId = EmployeeSubtypeId,
            Status = 1
        };
        var createResponse = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/operations/resources", newPerson);
        var createdResource = await createResponse.Content.ReadFromJsonAsync<ResourceDto>(_jsonOptions);

        // Act
        var response = await _client.DeleteAsync($"/api/organizations/{OrgId}/operations/resources/{createdResource!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/organizations/{OrgId}/operations/resources/{createdResource.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

// DTOs for Operations tests
public record ResourceSubtypeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public int ResourceType { get; init; }
    public string? Icon { get; init; }
    public Guid OrganizationId { get; init; }
    public DateTime CreatedAt { get; init; }
    public int ResourceCount { get; init; }
}

public record ResourceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public int Status { get; init; }
    public string? Metadata { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ResourceSubtypeId { get; init; }
    public string ResourceSubtypeName { get; init; } = "";
    public int ResourceType { get; init; }
    public Guid? LinkedUserId { get; init; }
    public string? LinkedUserName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int RoleAssignmentCount { get; init; }
    public int FunctionCapabilityCount { get; init; }
}
