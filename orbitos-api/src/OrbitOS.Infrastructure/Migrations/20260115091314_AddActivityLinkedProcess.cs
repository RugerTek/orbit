using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrbitOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLinkedProcess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LinkedProcessId",
                table: "Activities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_LinkedProcessId",
                table: "Activities",
                column: "LinkedProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Processes_LinkedProcessId",
                table: "Activities",
                column: "LinkedProcessId",
                principalTable: "Processes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Processes_LinkedProcessId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_LinkedProcessId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "LinkedProcessId",
                table: "Activities");
        }
    }
}
