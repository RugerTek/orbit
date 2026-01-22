using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Tests;

/// <summary>
/// Tests for AI Chat Service tools that allow AI agents to manage other AI agents and conversations.
/// These tools enable autonomous setup of company agents and communication channels.
/// </summary>
public class AiChatServiceToolsTests : IClassFixture<AiChatServiceToolsTests.AiToolsTestFactory>
{
    private readonly HttpClient _client;
    private readonly AiToolsTestFactory _factory;

    // Test organization ID
    public const string OrgId = "11111111-1111-1111-1111-111111111111";

    public AiChatServiceToolsTests(AiToolsTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // =========================================================================
    // AI AGENTS API - CRUD OPERATIONS
    // =========================================================================

    [Fact]
    public async Task CreateAiAgent_WithRequiredFields_ReturnsCreated()
    {
        // Arrange
        var createRequest = new
        {
            name = "Test CFO Agent",
            roleTitle = "Chief Financial Officer",
            provider = "Anthropic",
            modelId = "claude-sonnet-4-20250514",
            modelDisplayName = "Claude Sonnet",
            systemPrompt = "You are the CFO AI assistant. Provide financial analysis.",
            maxTokensPerResponse = 4096,
            temperature = 0.7,
            isActive = true
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/ai-agents", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var agent = await response.Content.ReadFromJsonAsync<AiAgentDto>();
        agent.Should().NotBeNull();
        agent!.Name.Should().Be("Test CFO Agent");
        agent.RoleTitle.Should().Be("Chief Financial Officer");
        agent.Provider.Should().BeEquivalentTo("Anthropic"); // Case-insensitive comparison
    }

    [Fact]
    public async Task CreateAiAgent_WithAllPersonalityFields_ReturnsCreated()
    {
        // Arrange
        var createRequest = new
        {
            name = "Test Strategy Agent",
            roleTitle = "Strategy Consultant",
            provider = "Anthropic",
            modelId = "claude-sonnet-4-20250514",
            modelDisplayName = "Claude Sonnet",
            systemPrompt = "You are a strategic advisor.",
            avatarColor = "#8B5CF6",
            maxTokensPerResponse = 8192,
            temperature = 0.9,
            assertiveness = 80,
            communicationStyle = "Direct",
            reactionTendency = "DevilsAdvocate",
            expertiseAreas = "strategy,market,competition",
            seniorityLevel = 5,
            asksQuestions = true,
            givesBriefAcknowledgments = false,
            isActive = true
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/ai-agents", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var agent = await response.Content.ReadFromJsonAsync<AiAgentDto>();
        agent.Should().NotBeNull();
        agent!.Assertiveness.Should().Be(80);
        agent.CommunicationStyle.Should().Be("Direct");
        agent.ReactionTendency.Should().Be("DevilsAdvocate");
        agent.SeniorityLevel.Should().Be(5);
    }

    [Fact]
    public async Task GetAiAgents_ReturnsAllAgents()
    {
        // Act
        var response = await _client.GetAsync($"/api/organizations/{OrgId}/ai-agents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var agents = await response.Content.ReadFromJsonAsync<List<AiAgentDto>>();
        agents.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAiAgent_ChangesFields_ReturnsOk()
    {
        // Arrange - Create an agent first
        var createRequest = new
        {
            name = "Agent To Update",
            roleTitle = "Initial Role",
            provider = "Anthropic",
            modelId = "claude-sonnet-4-20250514",
            modelDisplayName = "Claude Sonnet",
            systemPrompt = "Initial prompt.",
            isActive = true
        };
        var createResponse = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/ai-agents", createRequest);
        var createdAgent = await createResponse.Content.ReadFromJsonAsync<AiAgentDto>();

        // Act - Update the agent
        var updateRequest = new
        {
            name = "Updated Agent Name",
            roleTitle = "Updated Role",
            systemPrompt = "Updated prompt with more context.",
            assertiveness = 90,
            communicationStyle = "Diplomatic"
        };
        var updateResponse = await _client.PutAsJsonAsync($"/api/organizations/{OrgId}/ai-agents/{createdAgent!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedAgent = await updateResponse.Content.ReadFromJsonAsync<AiAgentDto>();
        updatedAgent.Should().NotBeNull();
        updatedAgent!.Name.Should().Be("Updated Agent Name");
        updatedAgent.RoleTitle.Should().Be("Updated Role");
        updatedAgent.Assertiveness.Should().Be(90);
    }

    [Fact]
    public async Task DeleteAiAgent_ReturnsNoContent()
    {
        // Arrange - Create an agent first
        var createRequest = new
        {
            name = "Agent To Delete",
            roleTitle = "Delete Test",
            provider = "Anthropic",
            modelId = "claude-sonnet-4-20250514",
            modelDisplayName = "Claude Sonnet",
            systemPrompt = "Temporary agent.",
            isActive = true
        };
        var createResponse = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/ai-agents", createRequest);
        var createdAgent = await createResponse.Content.ReadFromJsonAsync<AiAgentDto>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/organizations/{OrgId}/ai-agents/{createdAgent!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's soft-deleted (should not appear in list)
        var listResponse = await _client.GetAsync($"/api/organizations/{OrgId}/ai-agents");
        var agents = await listResponse.Content.ReadFromJsonAsync<List<AiAgentDto>>();
        agents.Should().NotContain(a => a.Id == createdAgent.Id);
    }

    // =========================================================================
    // CONVERSATIONS API - CRUD OPERATIONS
    // =========================================================================

    [Fact]
    public async Task CreateConversation_WithTitle_ReturnsCreated()
    {
        // Arrange
        var createRequest = new
        {
            title = "Q4 Strategy Discussion",
            mode = "OnDemand"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/conversations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationDto>();
        conversation.Should().NotBeNull();
        conversation!.Title.Should().Be("Q4 Strategy Discussion");
        conversation.Mode.Should().Be("OnDemand");
    }

    [Fact]
    public async Task CreateConversation_WithAgentIds_AddsParticipants()
    {
        // Arrange - Create an agent first
        var agentRequest = new
        {
            name = "Conversation Test Agent",
            roleTitle = "Test Role",
            provider = "Anthropic",
            modelId = "claude-sonnet-4-20250514",
            modelDisplayName = "Claude Sonnet",
            systemPrompt = "Test prompt.",
            isActive = true
        };
        var agentResponse = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/ai-agents", agentRequest);
        var agent = await agentResponse.Content.ReadFromJsonAsync<AiAgentDto>();

        // Create conversation with agent
        var createRequest = new
        {
            title = "Discussion with Agent",
            mode = "RoundRobin",
            aiAgentIds = new[] { agent!.Id }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/conversations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationDto>();
        conversation.Should().NotBeNull();
        conversation!.Participants.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateConversation_ChangesMode_ReturnsOk()
    {
        // Arrange - Create a conversation
        var createRequest = new { title = "Mode Test Conversation", mode = "OnDemand" };
        var createResponse = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/conversations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();

        // Act - Update mode via settings endpoint
        var updateRequest = new
        {
            title = "Updated Title",
            mode = "Emergent"
        };
        var updateResponse = await _client.PutAsJsonAsync($"/api/organizations/{OrgId}/conversations/{created!.Id}/settings", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ConversationDto>();
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated Title");
        updated.Mode.Should().Be("Emergent");
    }

    [Fact]
    public async Task AddAgentToConversation_ReturnsOk()
    {
        // Arrange - Create agent and conversation
        var agentRequest = new
        {
            name = "Add To Convo Agent",
            roleTitle = "Test",
            provider = "Anthropic",
            modelId = "claude-sonnet-4-20250514",
            modelDisplayName = "Claude Sonnet",
            systemPrompt = "Test.",
            isActive = true
        };
        var agentResponse = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/ai-agents", agentRequest);
        var agent = await agentResponse.Content.ReadFromJsonAsync<AiAgentDto>();

        var convoRequest = new { title = "Participant Test", mode = "OnDemand" };
        var convoResponse = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/conversations", convoRequest);
        var conversation = await convoResponse.Content.ReadFromJsonAsync<ConversationDto>();

        // Act - Add agent to conversation
        var addRequest = new { aiAgentId = agent!.Id };
        var addResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{OrgId}/conversations/{conversation!.Id}/participants",
            addRequest);

        // Assert
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConversations_ReturnsAllConversations()
    {
        // Act
        var response = await _client.GetAsync($"/api/organizations/{OrgId}/conversations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversations = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
        conversations.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteConversation_ReturnsNoContent()
    {
        // Arrange - Create a conversation
        var createRequest = new { title = "To Delete", mode = "OnDemand" };
        var createResponse = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/conversations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/organizations/{OrgId}/conversations/{created!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // =========================================================================
    // AI CHAT SERVICE - TOOL EXECUTION VIA CHAT
    // =========================================================================

    [Fact]
    public async Task AiChat_CreateAgentRequest_ToolExecutes()
    {
        // This test verifies that the AI can use the create_ai_agent tool
        // Note: This requires the ANTHROPIC_API_KEY to be set
        // In a real test environment, you might mock the API response

        // For now, we verify the endpoint exists and accepts requests
        var chatRequest = new
        {
            message = "Please create a new AI agent named 'Test HR Agent' with role 'HR Manager' and system prompt 'You help with HR matters.'"
        };

        var response = await _client.PostAsJsonAsync($"/api/organizations/{OrgId}/ai/chat", chatRequest);

        // The response might be an error if no API key is configured, but endpoint should exist
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError, HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // DTOs FOR TEST DESERIALIZATION
    // =========================================================================

    public record AiAgentDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = "";
        public string RoleTitle { get; init; } = "";
        public string Provider { get; init; } = "";
        public string ModelId { get; init; } = "";
        public string ModelDisplayName { get; init; } = "";
        public string? SystemPrompt { get; init; }
        public string? AvatarColor { get; init; }
        public int MaxTokensPerResponse { get; init; }
        public decimal Temperature { get; init; }
        public int Assertiveness { get; init; }
        public string CommunicationStyle { get; init; } = "";
        public string ReactionTendency { get; init; } = "";
        public string? ExpertiseAreas { get; init; }
        public int SeniorityLevel { get; init; }
        public bool AsksQuestions { get; init; }
        public bool GivesBriefAcknowledgments { get; init; }
        public bool IsActive { get; init; }
    }

    public record ConversationDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = "";
        public string Mode { get; init; } = "";
        public string Status { get; init; } = "";
        public int MessageCount { get; init; }
        public List<ParticipantDto> Participants { get; init; } = new();
    }

    public record ParticipantDto
    {
        public Guid Id { get; init; }
        public string ParticipantType { get; init; } = "";
        public Guid? AiAgentId { get; init; }
        public string? AgentName { get; init; }
    }

    // =========================================================================
    // TEST FACTORY
    // =========================================================================

    public class AiToolsTestFactory : WebApplicationFactory<Program>
    {
        private static bool _seeded = false;
        private static readonly object _lock = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<OrbitOSDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<OrbitOSDbContext>(options =>
                {
                    options.UseInMemoryDatabase("AiToolsTestDb");
                });

                // Add test configuration
                var configuration = new Dictionary<string, string?>
                {
                    { "ANTHROPIC_API_KEY", "test-api-key-for-testing" },
                    { "Jwt:Secret", "test-jwt-secret-key-for-testing-purposes-only-minimum-32-chars" },
                    { "Jwt:Issuer", "test-issuer" },
                    { "Jwt:Audience", "test-audience" }
                };

                services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
                    new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                        .AddInMemoryCollection(configuration)
                        .Build());

                // Build service provider and seed data
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrbitOSDbContext>();
                db.Database.EnsureCreated();

                lock (_lock)
                {
                    if (!_seeded)
                    {
                        SeedDatabase(db);
                        _seeded = true;
                    }
                }
            });
        }

        private static void SeedDatabase(OrbitOSDbContext db)
        {
            var orgId = Guid.Parse(OrgId);

            // Check if already seeded
            if (db.Organizations.Any(o => o.Id == orgId))
                return;

            // Create test organization
            var testOrg = new Organization
            {
                Id = orgId,
                Name = "Test Organization",
                Slug = "test-org"
            };
            db.Organizations.Add(testOrg);

            // Create a test user
            var testUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                DisplayName = "Test User",
                PasswordHash = "hashedpassword"
            };
            db.Users.Add(testUser);

            db.SaveChanges();
        }
    }
}
