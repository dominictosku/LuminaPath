using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuestFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuestFolderId",
                table: "Quests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuestFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    SectionName = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LuminaUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestFolders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quests_QuestFolderId",
                table: "Quests",
                column: "QuestFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestFolders_LuminaUserId_SortOrder",
                table: "QuestFolders",
                columns: new[] { "LuminaUserId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_QuestFolders_QuestFolderId",
                table: "Quests",
                column: "QuestFolderId",
                principalTable: "QuestFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quests_QuestFolders_QuestFolderId",
                table: "Quests");

            migrationBuilder.DropTable(
                name: "QuestFolders");

            migrationBuilder.DropIndex(
                name: "IX_Quests_QuestFolderId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "QuestFolderId",
                table: "Quests");
        }
    }
}
