using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Weeklyshedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEndAt",
                table: "Quests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledStartAt",
                table: "Quests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quests_LuminaUserId_ScheduledStartAt",
                table: "Quests",
                columns: new[] { "LuminaUserId", "ScheduledStartAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quests_LuminaUserId_ScheduledStartAt",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "ScheduledEndAt",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "ScheduledStartAt",
                table: "Quests");
        }
    }
}
