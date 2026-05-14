using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuestRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Recurrence",
                table: "Quests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Recurrence",
                table: "Quests");
        }
    }
}
