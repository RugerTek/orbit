using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrbitOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentPersonalityTraits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AsksQuestions",
                table: "AiAgents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Assertiveness",
                table: "AiAgents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CommunicationStyle",
                table: "AiAgents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExpertiseAreas",
                table: "AiAgents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GivesBriefAcknowledgments",
                table: "AiAgents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReactionTendency",
                table: "AiAgents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SeniorityLevel",
                table: "AiAgents",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AsksQuestions",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "Assertiveness",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "CommunicationStyle",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "ExpertiseAreas",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "GivesBriefAcknowledgments",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "ReactionTendency",
                table: "AiAgents");

            migrationBuilder.DropColumn(
                name: "SeniorityLevel",
                table: "AiAgents");
        }
    }
}
