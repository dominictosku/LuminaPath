using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuestBoardTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quests_AspNetUsers_OwnerId",
                table: "Quests");

            migrationBuilder.DropForeignKey(
                name: "FK_Quests_Media_GamesId",
                table: "Quests");

            migrationBuilder.DropIndex(
                name: "IX_Quests_GamesId",
                table: "Quests");

            migrationBuilder.DropIndex(
                name: "IX_Quests_OwnerId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "GamesId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Quests");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Quests",
                newName: "CompletedAt");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Quests",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "HasStartDate",
                table: "Quests",
                newName: "Completed");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Quests",
                newName: "LuminaUserId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Quests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RewardXp",
                table: "Quests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Quests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Quests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "QuestProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TotalXp = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LuminaUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestProfiles_AspNetUsers_LuminaUserId",
                        column: x => x.LuminaUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Xp = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    LuminaUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestSkills_AspNetUsers_LuminaUserId",
                        column: x => x.LuminaUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestSkillNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Unlocked = table.Column<bool>(type: "boolean", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    QuestSkillId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestSkillNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestSkillNodes_QuestSkills_QuestSkillId",
                        column: x => x.QuestSkillId,
                        principalTable: "QuestSkills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quests_LuminaUserId",
                table: "Quests",
                column: "LuminaUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestProfiles_LuminaUserId",
                table: "QuestProfiles",
                column: "LuminaUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestSkillNodes_QuestSkillId",
                table: "QuestSkillNodes",
                column: "QuestSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestSkills_LuminaUserId",
                table: "QuestSkills",
                column: "LuminaUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_AspNetUsers_LuminaUserId",
                table: "Quests",
                column: "LuminaUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quests_AspNetUsers_LuminaUserId",
                table: "Quests");

            migrationBuilder.DropTable(
                name: "QuestProfiles");

            migrationBuilder.DropTable(
                name: "QuestSkillNodes");

            migrationBuilder.DropTable(
                name: "QuestSkills");

            migrationBuilder.DropIndex(
                name: "IX_Quests_LuminaUserId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "RewardXp",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Quests");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Quests",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "LuminaUserId",
                table: "Quests",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "Quests",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "Completed",
                table: "Quests",
                newName: "HasStartDate");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Quests",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "GamesId",
                table: "Quests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Quests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Quests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quests_GamesId",
                table: "Quests",
                column: "GamesId");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_OwnerId",
                table: "Quests",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_AspNetUsers_OwnerId",
                table: "Quests",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_Media_GamesId",
                table: "Quests",
                column: "GamesId",
                principalTable: "Media",
                principalColumn: "Id");
        }
    }
}
