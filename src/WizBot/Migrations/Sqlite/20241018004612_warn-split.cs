using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    /// <inheritdoc />
    public partial class warnsplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "GuildId",
                table: "WarningPunishment",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);
            
            MigrationQueries.AddGuildIdsToWarningPunishment(migrationBuilder);
            
            migrationBuilder.DropForeignKey(
                name: "FK_WarningPunishment_GuildConfigs_GuildConfigId",
                table: "WarningPunishment");

            migrationBuilder.DropIndex(
                name: "IX_WarningPunishment_GuildConfigId",
                table: "WarningPunishment");

            migrationBuilder.DropColumn(
                name: "GuildConfigId",
                table: "WarningPunishment");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_WarningPunishment_GuildId_Count",
                table: "WarningPunishment",
                columns: new[] { "GuildId", "Count" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_WarningPunishment_GuildId_Count",
                table: "WarningPunishment");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "WarningPunishment");

            migrationBuilder.AddColumn<int>(
                name: "GuildConfigId",
                table: "WarningPunishment",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarningPunishment_GuildConfigId",
                table: "WarningPunishment",
                column: "GuildConfigId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarningPunishment_GuildConfigs_GuildConfigId",
                table: "WarningPunishment",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}