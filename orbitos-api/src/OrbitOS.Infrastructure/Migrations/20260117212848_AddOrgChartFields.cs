using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrbitOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgChartFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resources_OrganizationId",
                table: "Resources");

            migrationBuilder.AddColumn<bool>(
                name: "IsVacant",
                table: "Resources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ReportsToResourceId",
                table: "Resources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VacantPositionTitle",
                table: "Resources",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_OrganizationId_IsVacant",
                table: "Resources",
                columns: new[] { "OrganizationId", "IsVacant" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ReportsToResourceId",
                table: "Resources",
                column: "ReportsToResourceId",
                filter: "[ReportsToResourceId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Resources_ReportsToResourceId",
                table: "Resources",
                column: "ReportsToResourceId",
                principalTable: "Resources",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Resources_ReportsToResourceId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_OrganizationId_IsVacant",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_ReportsToResourceId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "IsVacant",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ReportsToResourceId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "VacantPositionTitle",
                table: "Resources");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_OrganizationId",
                table: "Resources",
                column: "OrganizationId");
        }
    }
}
