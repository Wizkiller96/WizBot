using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WizBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class awardedxptemprolenotify : Migration
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

            migrationBuilder.DropColumn(
                name: "notifyonlevelup",
                table: "userxpstats");

            migrationBuilder.CreateTable(
                name: "notify",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notify", x => x.id);
                    table.UniqueConstraint("ak_notify_guildid_type", x => new { x.guildid, x.type });
                });

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
                name: "notify");

            migrationBuilder.DropTable(
                name: "temprole");

            migrationBuilder.AddColumn<long>(
                name: "awardedxp",
                table: "userxpstats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "notifyonlevelup",
                table: "userxpstats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_userxpstats_awardedxp",
                table: "userxpstats",
                column: "awardedxp");
        }
    }
}