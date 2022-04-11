using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class rerocascade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReactionRole_ReactionRoleMessage_ReactionRoleMessageId",
                table: "ReactionRole");

            migrationBuilder.AddForeignKey(
                name: "FK_ReactionRole_ReactionRoleMessage_ReactionRoleMessageId",
                table: "ReactionRole",
                column: "ReactionRoleMessageId",
                principalTable: "ReactionRoleMessage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReactionRole_ReactionRoleMessage_ReactionRoleMessageId",
                table: "ReactionRole");

            migrationBuilder.AddForeignKey(
                name: "FK_ReactionRole_ReactionRoleMessage_ReactionRoleMessageId",
                table: "ReactionRole",
                column: "ReactionRoleMessageId",
                principalTable: "ReactionRoleMessage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
