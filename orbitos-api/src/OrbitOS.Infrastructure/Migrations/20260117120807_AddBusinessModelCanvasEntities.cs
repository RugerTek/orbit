using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrbitOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessModelCanvasEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Canvases_OrganizationId",
                table: "Canvases");

            migrationBuilder.AddColumn<string>(
                name: "AiMetadataJson",
                table: "Canvases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Canvases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentCanvasId",
                table: "Canvases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Canvases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScopeType",
                table: "Canvases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SegmentId",
                table: "Canvases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Canvases",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Canvases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VersionNote",
                table: "Canvases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiInsightsJson",
                table: "CanvasBlocks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "CanvasBlocks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Update existing CanvasBlocks to inherit OrganizationId from their parent Canvas IMMEDIATELY
            // This must happen before creating any indexes or foreign keys on this column
            migrationBuilder.Sql(@"
                UPDATE cb
                SET cb.OrganizationId = c.OrganizationId
                FROM CanvasBlocks cb
                INNER JOIN Canvases c ON cb.CanvasId = c.Id
                WHERE cb.OrganizationId = '00000000-0000-0000-0000-000000000000'
            ");

            migrationBuilder.AddColumn<string>(
                name: "PositionJson",
                table: "CanvasBlocks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "CanvasBlocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SummaryNote",
                table: "CanvasBlocks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "CanvasBlocks",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StrategicValue = table.Column<int>(type: "int", nullable: false),
                    RelationshipStrength = table.Column<int>(type: "int", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ContactJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServicesProvidedJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServicesReceivedJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Partners_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PricingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeaturesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Segment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DemographicsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BehaviorsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NeedsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetricsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Segment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Segment_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Ownership = table.Column<int>(type: "int", nullable: false),
                    PhasesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetricsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntegrationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Channels_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Channels_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PurposeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TouchpointsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LifecycleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetricsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerRelationships_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerRelationships_Segment_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "Segment",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RevenueStreams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PricingMechanism = table.Column<int>(type: "int", nullable: false),
                    PricingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevenueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetricsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WillingnessToPayJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevenueStreams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevenueStreams_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevenueStreams_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevenueStreams_Segment_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "Segment",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ValuePropositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Headline = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CustomerJobsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PainsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GainsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PainRelieversJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GainCreatorsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductsServicesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DifferentiatorsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValuePropositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValuePropositions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValuePropositions_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValuePropositions_Segment_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "Segment",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BlockReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    LinkType = table.Column<int>(type: "int", nullable: false),
                    ContextNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    MetricsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanvasBlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerRelationshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevenueStreamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValuePropositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockReferences_CanvasBlocks_CanvasBlockId",
                        column: x => x.CanvasBlockId,
                        principalTable: "CanvasBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlockReferences_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BlockReferences_CustomerRelationships_CustomerRelationshipId",
                        column: x => x.CustomerRelationshipId,
                        principalTable: "CustomerRelationships",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BlockReferences_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BlockReferences_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BlockReferences_RevenueStreams_RevenueStreamId",
                        column: x => x.RevenueStreamId,
                        principalTable: "RevenueStreams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BlockReferences_ValuePropositions_ValuePropositionId",
                        column: x => x.ValuePropositionId,
                        principalTable: "ValuePropositions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Canvases_OrganizationId_ScopeType",
                table: "Canvases",
                columns: new[] { "OrganizationId", "ScopeType" });

            migrationBuilder.CreateIndex(
                name: "IX_Canvases_OrganizationId_Slug",
                table: "Canvases",
                columns: new[] { "OrganizationId", "Slug" },
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Canvases_ParentCanvasId",
                table: "Canvases",
                column: "ParentCanvasId",
                filter: "[ParentCanvasId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Canvases_ProductId",
                table: "Canvases",
                column: "ProductId",
                filter: "[ProductId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Canvases_SegmentId",
                table: "Canvases",
                column: "SegmentId",
                filter: "[SegmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CanvasBlocks_OrganizationId",
                table: "CanvasBlocks",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockReferences_CanvasBlockId_EntityType_EntityId",
                table: "BlockReferences",
                columns: new[] { "CanvasBlockId", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockReferences_ChannelId",
                table: "BlockReferences",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockReferences_CustomerRelationshipId",
                table: "BlockReferences",
                column: "CustomerRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockReferences_EntityType_EntityId",
                table: "BlockReferences",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_BlockReferences_OrganizationId",
                table: "BlockReferences",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockReferences_PartnerId",
                table: "BlockReferences",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockReferences_RevenueStreamId",
                table: "BlockReferences",
                column: "RevenueStreamId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockReferences_ValuePropositionId",
                table: "BlockReferences",
                column: "ValuePropositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_OrganizationId_Category",
                table: "Channels",
                columns: new[] { "OrganizationId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Channels_OrganizationId_Slug",
                table: "Channels",
                columns: new[] { "OrganizationId", "Slug" },
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_OrganizationId_Status",
                table: "Channels",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Channels_OrganizationId_Type",
                table: "Channels",
                columns: new[] { "OrganizationId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Channels_PartnerId",
                table: "Channels",
                column: "PartnerId",
                filter: "[PartnerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRelationships_OrganizationId_Slug",
                table: "CustomerRelationships",
                columns: new[] { "OrganizationId", "Slug" },
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRelationships_OrganizationId_Status",
                table: "CustomerRelationships",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRelationships_OrganizationId_Type",
                table: "CustomerRelationships",
                columns: new[] { "OrganizationId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRelationships_SegmentId",
                table: "CustomerRelationships",
                column: "SegmentId",
                filter: "[SegmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_OrganizationId_Slug",
                table: "Partners",
                columns: new[] { "OrganizationId", "Slug" },
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_OrganizationId_Status",
                table: "Partners",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Partners_OrganizationId_Type",
                table: "Partners",
                columns: new[] { "OrganizationId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_OrganizationId",
                table: "Product",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueStreams_OrganizationId_Slug",
                table: "RevenueStreams",
                columns: new[] { "OrganizationId", "Slug" },
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueStreams_OrganizationId_Status",
                table: "RevenueStreams",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RevenueStreams_OrganizationId_Type",
                table: "RevenueStreams",
                columns: new[] { "OrganizationId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_RevenueStreams_ProductId",
                table: "RevenueStreams",
                column: "ProductId",
                filter: "[ProductId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueStreams_SegmentId",
                table: "RevenueStreams",
                column: "SegmentId",
                filter: "[SegmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Segment_OrganizationId",
                table: "Segment",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ValuePropositions_OrganizationId_Slug",
                table: "ValuePropositions",
                columns: new[] { "OrganizationId", "Slug" },
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ValuePropositions_OrganizationId_Status",
                table: "ValuePropositions",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ValuePropositions_ProductId",
                table: "ValuePropositions",
                column: "ProductId",
                filter: "[ProductId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ValuePropositions_SegmentId",
                table: "ValuePropositions",
                column: "SegmentId",
                filter: "[SegmentId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CanvasBlocks_Organizations_OrganizationId",
                table: "CanvasBlocks",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Canvases_Canvases_ParentCanvasId",
                table: "Canvases",
                column: "ParentCanvasId",
                principalTable: "Canvases",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Canvases_Product_ProductId",
                table: "Canvases",
                column: "ProductId",
                principalTable: "Product",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Canvases_Segment_SegmentId",
                table: "Canvases",
                column: "SegmentId",
                principalTable: "Segment",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CanvasBlocks_Organizations_OrganizationId",
                table: "CanvasBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Canvases_Canvases_ParentCanvasId",
                table: "Canvases");

            migrationBuilder.DropForeignKey(
                name: "FK_Canvases_Product_ProductId",
                table: "Canvases");

            migrationBuilder.DropForeignKey(
                name: "FK_Canvases_Segment_SegmentId",
                table: "Canvases");

            migrationBuilder.DropTable(
                name: "BlockReferences");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "CustomerRelationships");

            migrationBuilder.DropTable(
                name: "RevenueStreams");

            migrationBuilder.DropTable(
                name: "ValuePropositions");

            migrationBuilder.DropTable(
                name: "Partners");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Segment");

            migrationBuilder.DropIndex(
                name: "IX_Canvases_OrganizationId_ScopeType",
                table: "Canvases");

            migrationBuilder.DropIndex(
                name: "IX_Canvases_OrganizationId_Slug",
                table: "Canvases");

            migrationBuilder.DropIndex(
                name: "IX_Canvases_ParentCanvasId",
                table: "Canvases");

            migrationBuilder.DropIndex(
                name: "IX_Canvases_ProductId",
                table: "Canvases");

            migrationBuilder.DropIndex(
                name: "IX_Canvases_SegmentId",
                table: "Canvases");

            migrationBuilder.DropIndex(
                name: "IX_CanvasBlocks_OrganizationId",
                table: "CanvasBlocks");

            migrationBuilder.DropColumn(
                name: "AiMetadataJson",
                table: "Canvases");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Canvases");

            migrationBuilder.DropColumn(
                name: "ParentCanvasId",
                table: "Canvases");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Canvases");

            migrationBuilder.DropColumn(
                name: "ScopeType",
                table: "Canvases");

            migrationBuilder.DropColumn(
                name: "SegmentId",
                table: "Canvases");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Canvases");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Canvases");

            migrationBuilder.DropColumn(
                name: "VersionNote",
                table: "Canvases");

            migrationBuilder.DropColumn(
                name: "AiInsightsJson",
                table: "CanvasBlocks");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "CanvasBlocks");

            migrationBuilder.DropColumn(
                name: "PositionJson",
                table: "CanvasBlocks");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "CanvasBlocks");

            migrationBuilder.DropColumn(
                name: "SummaryNote",
                table: "CanvasBlocks");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "CanvasBlocks");

            migrationBuilder.CreateIndex(
                name: "IX_Canvases_OrganizationId",
                table: "Canvases",
                column: "OrganizationId");
        }
    }
}
