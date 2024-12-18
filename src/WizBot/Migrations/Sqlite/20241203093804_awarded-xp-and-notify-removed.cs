using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    /// <inheritdoc />
    public partial class awardedxpandnotifyremoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserXpStats_AwardedXp",
                table: "UserXpStats");

            migrationBuilder.DropColumn(
                name: "AwardedXp",
                table: "UserXpStats");

            migrationBuilder.CreateTable(
                name: "TempRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Remove = table.Column<bool>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TempRole", x => x.Id);
                    table.UniqueConstraint("AK_TempRole_GuildId_UserId_RoleId", x => new { x.GuildId, x.UserId, x.RoleId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_TempRole_ExpiresAt",
                table: "TempRole",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TempRole");

            migrationBuilder.AddColumn<long>(
                name: "AwardedXp",
                table: "UserXpStats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_UserXpStats_AwardedXp",
                table: "UserXpStats",
                column: "AwardedXp");
        }
    }
}