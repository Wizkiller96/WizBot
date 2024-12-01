using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WizBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class btnroles_guildcolors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "buttonrole",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    buttonid = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    messageid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    emote = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    exclusive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buttonrole", x => x.id);
                    table.UniqueConstraint("ak_buttonrole_roleid_messageid", x => new { x.roleid, x.messageid });
                });

            migrationBuilder.CreateTable(
                name: "guildcolors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    okcolor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    errorcolor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    pendingcolor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guildcolors", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_buttonrole_guildid",
                table: "buttonrole",
                column: "guildid");

            migrationBuilder.CreateIndex(
                name: "ix_guildcolors_guildid",
                table: "guildcolors",
                column: "guildid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "buttonrole");

            migrationBuilder.DropTable(
                name: "guildcolors");
        }
    }
}
