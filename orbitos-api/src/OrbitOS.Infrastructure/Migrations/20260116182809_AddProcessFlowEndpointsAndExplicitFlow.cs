using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrbitOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessFlowEndpointsAndExplicitFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL to conditionally add columns (handles partial migration cleanup)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'EntryActivityId' AND object_id = OBJECT_ID('Processes'))
                BEGIN
                    ALTER TABLE [Processes] ADD [EntryActivityId] uniqueidentifier NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'ExitActivityId' AND object_id = OBJECT_ID('Processes'))
                BEGIN
                    ALTER TABLE [Processes] ADD [ExitActivityId] uniqueidentifier NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'UseExplicitFlow' AND object_id = OBJECT_ID('Processes'))
                BEGIN
                    ALTER TABLE [Processes] ADD [UseExplicitFlow] bit NOT NULL DEFAULT 0;
                END
            ");

            // Create indexes if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Processes_EntryActivityId' AND object_id = OBJECT_ID('Processes'))
                BEGIN
                    CREATE INDEX [IX_Processes_EntryActivityId] ON [Processes] ([EntryActivityId]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Processes_ExitActivityId' AND object_id = OBJECT_ID('Processes'))
                BEGIN
                    CREATE INDEX [IX_Processes_ExitActivityId] ON [Processes] ([ExitActivityId]);
                END
            ");

            // Add foreign keys if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Processes_Activities_EntryActivityId')
                BEGIN
                    ALTER TABLE [Processes] ADD CONSTRAINT [FK_Processes_Activities_EntryActivityId]
                        FOREIGN KEY ([EntryActivityId]) REFERENCES [Activities] ([Id]) ON DELETE NO ACTION;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Processes_Activities_ExitActivityId')
                BEGIN
                    ALTER TABLE [Processes] ADD CONSTRAINT [FK_Processes_Activities_ExitActivityId]
                        FOREIGN KEY ([ExitActivityId]) REFERENCES [Activities] ([Id]) ON DELETE NO ACTION;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Processes_Activities_EntryActivityId",
                table: "Processes");

            migrationBuilder.DropForeignKey(
                name: "FK_Processes_Activities_ExitActivityId",
                table: "Processes");

            migrationBuilder.DropIndex(
                name: "IX_Processes_EntryActivityId",
                table: "Processes");

            migrationBuilder.DropIndex(
                name: "IX_Processes_ExitActivityId",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "EntryActivityId",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "ExitActivityId",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "UseExplicitFlow",
                table: "Processes");
        }
    }
}
