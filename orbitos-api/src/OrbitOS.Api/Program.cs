using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using OrbitOS.Application.Interfaces;
using OrbitOS.Infrastructure;
using OrbitOS.Infrastructure.Data;
using OrbitOS.Api.Services;
using OrbitOS.Api.Hubs;
using DotNetEnv;

// Load environment variables from .env in current/parent dirs for local development
try { Env.TraversePath().Load(); } catch { /* .env file not found - OK in production */ }

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// Add authentication - use AzureAd if configured, otherwise use JWT bearer
var azureAdSection = builder.Configuration.GetSection("AzureAd");
if (azureAdSection.Exists() && !string.IsNullOrEmpty(azureAdSection["ClientId"]))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(azureAdSection);
}
else
{
    // Use simple JWT bearer authentication for local/custom JWT tokens
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtKey = builder.Configuration["Jwt:Key"];
            if (!string.IsNullOrEmpty(jwtKey))
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "OrbitOS",
                    ValidAudience = builder.Configuration["Jwt:Audience"] ?? "OrbitOS",
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtKey))
                };
            }
        });
}

// Add authorization
builder.Services.AddAuthorization();

// Add infrastructure services (DbContext, CurrentUserService, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Add HttpClient for AI services
builder.Services.AddHttpClient("Anthropic");
builder.Services.AddHttpClient("OpenAI");
builder.Services.AddHttpClient("Google");

// Add AI services
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IDashboardInsightsService, DashboardInsightsService>();
builder.Services.AddScoped<IMultiProviderAiService, MultiProviderAiService>();
builder.Services.AddScoped<IOrganizationContextService, OrganizationContextService>();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Support string enum values from frontend (e.g., "BusinessModel" instead of 0)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR for real-time messaging
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Add CORS for frontend apps
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends", policy =>
    {
        policy.WithOrigins(
            builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>()
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

var autoMigrate = builder.Configuration.GetValue("Database:AutoMigrate", app.Environment.IsDevelopment());
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrbitOSDbContext>();
    try
    {
        dbContext.Database.Migrate();

        // Seed initial data after migrations
        var autoSeed = builder.Configuration.GetValue("Database:AutoSeed", app.Environment.IsDevelopment());
        if (autoSeed)
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
            await seeder.SeedAsync();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database migration or seeding failed");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontends");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hub for real-time conversations
app.MapHub<ConversationHub>("/hubs/conversations");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
