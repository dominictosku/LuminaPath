using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuestCRUDAndPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quests_LuminaUserId",
                table: "Quests");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Quests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Quests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Quests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Quests",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Quests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Quests_LuminaUserId_Completed_DueDate",
                table: "Quests",
                columns: new[] { "LuminaUserId", "Completed", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Quests_LuminaUserId_SortOrder",
                table: "Quests",
                columns: new[] { "LuminaUserId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quests_LuminaUserId_Completed_DueDate",
                table: "Quests");

            migrationBuilder.DropIndex(
                name: "IX_Quests_LuminaUserId_SortOrder",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Quests");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_LuminaUserId",
                table: "Quests",
                column: "LuminaUserId");
        }
    }
}
