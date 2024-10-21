using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class warnsplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "guildid",
                table: "warningpunishment",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            MigrationQueries.AddGuildIdsToWarningPunishment(migrationBuilder);

            migrationBuilder.DropForeignKey(
                name: "fk_warningpunishment_guildconfigs_guildconfigid",
                table: "warningpunishment");

            migrationBuilder.DropIndex(
                name: "ix_warningpunishment_guildconfigid",
                table: "warningpunishment");

            migrationBuilder.DropColumn(
                name: "guildconfigid",
                table: "warningpunishment");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_warningpunishment_guildid_count",
                table: "warningpunishment",
                columns: new[] { "guildid", "count" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "ak_warningpunishment_guildid_count",
                table: "warningpunishment");

            migrationBuilder.DropColumn(
                name: "guildid",
                table: "warningpunishment");

            migrationBuilder.AddColumn<int>(
                name: "guildconfigid",
                table: "warningpunishment",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_warningpunishment_guildconfigid",
                table: "warningpunishment",
                column: "guildconfigid");

            migrationBuilder.AddForeignKey(
                name: "fk_warningpunishment_guildconfigs_guildconfigid",
                table: "warningpunishment",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}