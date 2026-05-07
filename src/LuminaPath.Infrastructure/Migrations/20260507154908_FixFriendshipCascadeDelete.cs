using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFriendshipCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DirectMessages_AspNetUsers_RecipientId",
                table: "DirectMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_AspNetUsers_AddresseeId",
                table: "Friendships");

            migrationBuilder.AddForeignKey(
                name: "FK_DirectMessages_AspNetUsers_RecipientId",
                table: "DirectMessages",
                column: "RecipientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_AspNetUsers_AddresseeId",
                table: "Friendships",
                column: "AddresseeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DirectMessages_AspNetUsers_RecipientId",
                table: "DirectMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_AspNetUsers_AddresseeId",
                table: "Friendships");

            migrationBuilder.AddForeignKey(
                name: "FK_DirectMessages_AspNetUsers_RecipientId",
                table: "DirectMessages",
                column: "RecipientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_AspNetUsers_AddresseeId",
                table: "Friendships",
                column: "AddresseeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
