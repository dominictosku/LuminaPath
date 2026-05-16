using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GamePlatformAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameAchievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    CanonicalKey = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    IconUrl = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SteamApiName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    SteamDisplayName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    PsnTrophyId = table.Column<int>(type: "integer", nullable: true),
                    PsnGroupId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PsnTrophyType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PrimaryProvider = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameAchievements_Media_GameId",
                        column: x => x.GameId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGameAchievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameAchievementId = table.Column<int>(type: "integer", nullable: false),
                    LuminaUserId = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    SourceAchievementId = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGameAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGameAchievements_GameAchievements_GameAchievementId",
                        column: x => x.GameAchievementId,
                        principalTable: "GameAchievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameAchievements_GameId_CanonicalKey",
                table: "GameAchievements",
                columns: new[] { "GameId", "CanonicalKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameAchievements_GameId_PsnTrophyId_PsnGroupId",
                table: "GameAchievements",
                columns: new[] { "GameId", "PsnTrophyId", "PsnGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_GameAchievements_SteamApiName",
                table: "GameAchievements",
                column: "SteamApiName");

            migrationBuilder.CreateIndex(
                name: "IX_UserGameAchievements_GameAchievementId",
                table: "UserGameAchievements",
                column: "GameAchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGameAchievements_LuminaUserId_GameAchievementId",
                table: "UserGameAchievements",
                columns: new[] { "LuminaUserId", "GameAchievementId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserGameAchievements_LuminaUserId_GameAchievementId_Provide~",
                table: "UserGameAchievements",
                columns: new[] { "LuminaUserId", "GameAchievementId", "Provider", "SourceAchievementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserGameAchievements");

            migrationBuilder.DropTable(
                name: "GameAchievements");
        }
    }
}
