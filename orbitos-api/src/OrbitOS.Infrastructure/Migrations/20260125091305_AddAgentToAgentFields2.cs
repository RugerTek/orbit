using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrbitOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentToAgentFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentType",
                table: "AiAgents",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BasePrompt",
                table: "AiAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanBeOrchestrated",
                table: "AiAgents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanCallBuiltInAgents",
                table: "AiAgents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContextScopesJson",
                table: "AiAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomInstructions",
                table: "AiAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemProvided",
                table: "AiAgents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SpecialistKey",
                table: "AiAgents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiAgents_OrganizationId_AgentType",
                table: "AiAgents",
                columns: new[] { "OrganizationId", "AgentType" });

            migrationBuilder.CreateIndex(
                name: "IX_AiAgents_OrganizationId_SpecialistKey",
                table: "AiAgents",
                columns: new[] { "OrganizationId", "SpecialistKey" },
                filter: "[SpecialistKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiAgents_OrganizationId_AgentType",
                table: "AiAgents");

            migrationBuilder.DropIndex(
                name: "IX_AiAgents_OrganizationId_SpecialistKey",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "AgentType",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "BasePrompt",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "CanBeOrchestrated",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "CanCallBuiltInAgents",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "ContextScopesJson",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "CustomInstructions",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "IsSystemProvided",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "SpecialistKey",
                table: "AiAgents");
        }
    }
}
