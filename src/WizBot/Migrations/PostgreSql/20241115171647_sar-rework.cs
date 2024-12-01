using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WizBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class sarrework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "sarautodelete",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                              .Annotation("Npgsql:ValueGenerationStrategy",
                                  NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    isenabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sarautodelete", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sargroup",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                              .Annotation("Npgsql:ValueGenerationStrategy",
                                  NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    groupnumber = table.Column<int>(type: "integer", nullable: false),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    rolereq = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    isexclusive = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sargroup", x => x.id);
                    table.UniqueConstraint("ak_sargroup_guildid_groupnumber",
                        x => new
                        {
                            x.guildid,
                            x.groupnumber
                        });
                });

            migrationBuilder.CreateTable(
                name: "sar",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                              .Annotation("Npgsql:ValueGenerationStrategy",
                                  NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    sargroupid = table.Column<int>(type: "integer", nullable: false),
                    levelreq = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sar", x => x.id);
                    table.UniqueConstraint("ak_sar_guildid_roleid",
                        x => new
                        {
                            x.guildid,
                            x.roleid
                        });
                    table.ForeignKey(
                        name: "fk_sar_sargroup_sargroupid",
                        column: x => x.sargroupid,
                        principalTable: "sargroup",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sar_sargroupid",
                table: "sar",
                column: "sargroupid");

            migrationBuilder.CreateIndex(
                name: "ix_sarautodelete_guildid",
                table: "sarautodelete",
                column: "guildid",
                unique: true);
            
            MigrationQueries.MigrateSar(migrationBuilder);
            
            migrationBuilder.DropTable(
                name: "groupname");

            migrationBuilder.DropTable(
                name: "selfassignableroles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sar");

            migrationBuilder.DropTable(
                name: "sarautodelete");

            migrationBuilder.DropTable(
                name: "sargroup");

            migrationBuilder.CreateTable(
                name: "groupname",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                              .Annotation("Npgsql:ValueGenerationStrategy",
                                  NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildconfigid = table.Column<int>(type: "integer", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groupname", x => x.id);
                    table.ForeignKey(
                        name: "fk_groupname_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "selfassignableroles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                              .Annotation("Npgsql:ValueGenerationStrategy",
                                  NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    group = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    levelrequirement = table.Column<int>(type: "integer", nullable: false),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_selfassignableroles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_groupname_guildconfigid_number",
                table: "groupname",
                columns: new[] { "guildconfigid", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_selfassignableroles_guildid_roleid",
                table: "selfassignableroles",
                columns: new[] { "guildid", "roleid" },
                unique: true);
        }
    }
}