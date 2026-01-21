using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrbitOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSoftDeleteUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoleFunctions_RoleId_FunctionId",
                table: "RoleFunctions");

            migrationBuilder.DropIndex(
                name: "IX_RoleAssignments_ResourceId_RoleId",
                table: "RoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_FunctionCapabilities_ResourceId_FunctionId",
                table: "FunctionCapabilities");

            migrationBuilder.DropIndex(
                name: "IX_CanvasBlocks_CanvasId_BlockType",
                table: "CanvasBlocks");

            migrationBuilder.DropIndex(
                name: "IX_BlockReferences_CanvasBlockId_EntityType_EntityId",
                table: "BlockReferences");

            migrationBuilder.CreateIndex(
                name: "IX_RoleFunctions_RoleId_FunctionId",
                table: "RoleFunctions",
                columns: new[] { "RoleId", "FunctionId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_ResourceId_RoleId",
                table: "RoleAssignments",
                columns: new[] { "ResourceId", "RoleId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionCapabilities_ResourceId_FunctionId",
                table: "FunctionCapabilities",
                columns: new[] { "ResourceId", "FunctionId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CanvasBlocks_CanvasId_BlockType",
                table: "CanvasBlocks",
                columns: new[] { "CanvasId", "BlockType" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BlockReferences_CanvasBlockId_EntityType_EntityId",
                table: "BlockReferences",
                columns: new[] { "CanvasBlockId", "EntityType", "EntityId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoleFunctions_RoleId_FunctionId",
                table: "RoleFunctions");

            migrationBuilder.DropIndex(
                name: "IX_RoleAssignments_ResourceId_RoleId",
                table: "RoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_FunctionCapabilities_ResourceId_FunctionId",
                table: "FunctionCapabilities");

            migrationBuilder.DropIndex(
                name: "IX_CanvasBlocks_CanvasId_BlockType",
                table: "CanvasBlocks");

            migrationBuilder.DropIndex(
                name: "IX_BlockReferences_CanvasBlockId_EntityType_EntityId",
                table: "BlockReferences");

            migrationBuilder.CreateIndex(
                name: "IX_RoleFunctions_RoleId_FunctionId",
                table: "RoleFunctions",
                columns: new[] { "RoleId", "FunctionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_ResourceId_RoleId",
                table: "RoleAssignments",
                columns: new[] { "ResourceId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FunctionCapabilities_ResourceId_FunctionId",
                table: "FunctionCapabilities",
                columns: new[] { "ResourceId", "FunctionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanvasBlocks_CanvasId_BlockType",
                table: "CanvasBlocks",
                columns: new[] { "CanvasId", "BlockType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockReferences_CanvasBlockId_EntityType_EntityId",
                table: "BlockReferences",
                columns: new[] { "CanvasBlockId", "EntityType", "EntityId" },
                unique: true);
        }
    }
}
