using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WizBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class awardedxpandnotifyremoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_userxpstats_awardedxp",
                table: "userxpstats");

            migrationBuilder.DropColumn(
                name: "awardedxp",
                table: "userxpstats");

            migrationBuilder.CreateTable(
                name: "temprole",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    remove = table.Column<bool>(type: "boolean", nullable: false),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    expiresat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_temprole", x => x.id);
                    table.UniqueConstraint("ak_temprole_guildid_userid_roleid", x => new { x.guildid, x.userid, x.roleid });
                });

            migrationBuilder.CreateIndex(
                name: "ix_temprole_expiresat",
                table: "temprole",
                column: "expiresat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "temprole");

            migrationBuilder.AddColumn<long>(
                name: "awardedxp",
                table: "userxpstats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "ix_userxpstats_awardedxp",
                table: "userxpstats",
                column: "awardedxp");
        }
    }
}