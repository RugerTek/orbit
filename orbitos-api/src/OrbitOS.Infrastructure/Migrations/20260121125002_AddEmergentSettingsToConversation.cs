using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrbitOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmergentSettingsToConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmergentSettingsJson",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmergentSettingsJson",
                table: "Conversations");
        }
    }
}
