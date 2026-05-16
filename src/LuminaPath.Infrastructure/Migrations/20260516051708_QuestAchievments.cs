using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuestAchievments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<string>>(
                name: "Tags",
                table: "Quests",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<int>(
                name: "SkillId",
                table: "Quests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStreakDays",
                table: "QuestProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastCompletionDate",
                table: "QuestProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LongestStreakDays",
                table: "QuestProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    QuestProfileId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Achievements_QuestProfiles_QuestProfileId",
                        column: x => x.QuestProfileId,
                        principalTable: "QuestProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestSubtasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    QuestId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestSubtasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestSubtasks_Quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quests_SkillId",
                table: "Quests",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_QuestProfileId_Code",
                table: "Achievements",
                columns: new[] { "QuestProfileId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestSubtasks_QuestId_SortOrder",
                table: "QuestSubtasks",
                columns: new[] { "QuestId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_QuestSkills_SkillId",
                table: "Quests",
                column: "SkillId",
                principalTable: "QuestSkills",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quests_QuestSkills_SkillId",
                table: "Quests");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "QuestSubtasks");

            migrationBuilder.DropIndex(
                name: "IX_Quests_SkillId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "SkillId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "CurrentStreakDays",
                table: "QuestProfiles");

            migrationBuilder.DropColumn(
                name: "LastCompletionDate",
                table: "QuestProfiles");

            migrationBuilder.DropColumn(
                name: "LongestStreakDays",
                table: "QuestProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "Quests",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]");
        }
    }
}
