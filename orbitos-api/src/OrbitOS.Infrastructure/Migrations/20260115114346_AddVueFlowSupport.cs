using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrbitOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVueFlowSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PositionX",
                table: "Activities",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PositionY",
                table: "Activities",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "ActivityEdges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceHandle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetHandle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EdgeType = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Animated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEdges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityEdges_Activities_SourceActivityId",
                        column: x => x.SourceActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActivityEdges_Activities_TargetActivityId",
                        column: x => x.TargetActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActivityEdges_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEdges_ProcessId_SourceActivityId_TargetActivityId_SourceHandle",
                table: "ActivityEdges",
                columns: new[] { "ProcessId", "SourceActivityId", "TargetActivityId", "SourceHandle" },
                unique: true,
                filter: "[SourceHandle] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEdges_SourceActivityId",
                table: "ActivityEdges",
                column: "SourceActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEdges_TargetActivityId",
                table: "ActivityEdges",
                column: "TargetActivityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityEdges");

            migrationBuilder.DropColumn(
                name: "PositionX",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "PositionY",
                table: "Activities");
        }
    }
}
