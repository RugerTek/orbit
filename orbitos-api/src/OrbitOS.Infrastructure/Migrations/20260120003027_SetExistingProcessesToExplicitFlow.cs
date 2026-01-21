using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrbitOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetExistingProcessesToExplicitFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update all existing processes to use explicit flow mode
            // This prevents new activities from being auto-connected
            migrationBuilder.Sql("UPDATE [Processes] SET [UseExplicitFlow] = 1 WHERE [UseExplicitFlow] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
