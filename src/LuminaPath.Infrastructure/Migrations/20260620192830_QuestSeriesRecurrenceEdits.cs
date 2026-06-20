using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuestSeriesRecurrenceEdits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OverridesQuestSeries",
                table: "Quests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProjectsQuestSeries",
                table: "Quests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QuestSeriesId",
                table: "Quests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SeriesOccurrenceDate",
                table: "Quests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuestSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Recurrence = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledEndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tags = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    RewardXp = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LuminaUserId = table.Column<string>(type: "text", nullable: false),
                    MyGameId = table.Column<int>(type: "integer", nullable: true),
                    SkillId = table.Column<int>(type: "integer", nullable: true),
                    QuestFolderId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestSeries_AspNetUsers_LuminaUserId",
                        column: x => x.LuminaUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestSeries_MyGames_MyGameId",
                        column: x => x.MyGameId,
                        principalTable: "MyGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QuestSeries_QuestFolders_QuestFolderId",
                        column: x => x.QuestFolderId,
                        principalTable: "QuestFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QuestSeries_QuestSkills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "QuestSkills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quests_LuminaUserId_QuestSeriesId_SeriesOccurrenceDate",
                table: "Quests",
                columns: new[] { "LuminaUserId", "QuestSeriesId", "SeriesOccurrenceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Quests_QuestSeriesId",
                table: "Quests",
                column: "QuestSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestSeries_LuminaUserId_Recurrence",
                table: "QuestSeries",
                columns: new[] { "LuminaUserId", "Recurrence" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestSeries_LuminaUserId_ScheduledStartAt",
                table: "QuestSeries",
                columns: new[] { "LuminaUserId", "ScheduledStartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestSeries_MyGameId",
                table: "QuestSeries",
                column: "MyGameId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestSeries_QuestFolderId",
                table: "QuestSeries",
                column: "QuestFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestSeries_SkillId",
                table: "QuestSeries",
                column: "SkillId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_QuestSeries_QuestSeriesId",
                table: "Quests",
                column: "QuestSeriesId",
                principalTable: "QuestSeries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quests_QuestSeries_QuestSeriesId",
                table: "Quests");

            migrationBuilder.DropTable(
                name: "QuestSeries");

            migrationBuilder.DropIndex(
                name: "IX_Quests_LuminaUserId_QuestSeriesId_SeriesOccurrenceDate",
                table: "Quests");

            migrationBuilder.DropIndex(
                name: "IX_Quests_QuestSeriesId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "OverridesQuestSeries",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "ProjectsQuestSeries",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "QuestSeriesId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "SeriesOccurrenceDate",
                table: "Quests");
        }
    }
}
