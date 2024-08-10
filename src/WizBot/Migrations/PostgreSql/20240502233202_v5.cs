using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WizBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class v5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nsfwblacklistedtags");

            migrationBuilder.DropTable(
                name: "pollanswer");

            migrationBuilder.DropTable(
                name: "pollvote");

            migrationBuilder.DropTable(
                name: "poll");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_clubs_name",
                table: "clubs");

            migrationBuilder.AddColumn<string>(
                name: "command",
                table: "shopentry",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "reminders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "stickyroles",
                table: "guildconfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "clubs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "giveawaymodel",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    messageid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    endsat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_giveawaymodel", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stickyroles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    roleids = table.Column<string>(type: "text", nullable: true),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stickyroles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "todosarchive",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_todosarchive", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "giveawayuser",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    giveawayid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_giveawayuser", x => x.id);
                    table.ForeignKey(
                        name: "fk_giveawayuser_giveawaymodel_giveawayid",
                        column: x => x.giveawayid,
                        principalTable: "giveawaymodel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "todos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    todo = table.Column<string>(type: "text", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    isdone = table.Column<bool>(type: "boolean", nullable: false),
                    archiveid = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_todos", x => x.id);
                    table.ForeignKey(
                        name: "fk_todos_todosarchive_archiveid",
                        column: x => x.archiveid,
                        principalTable: "todosarchive",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clubs_name",
                table: "clubs",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_giveawayuser_giveawayid_userid",
                table: "giveawayuser",
                columns: new[] { "giveawayid", "userid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stickyroles_guildid_userid",
                table: "stickyroles",
                columns: new[] { "guildid", "userid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_todos_archiveid",
                table: "todos",
                column: "archiveid");

            migrationBuilder.CreateIndex(
                name: "ix_todos_userid",
                table: "todos",
                column: "userid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "giveawayuser");

            migrationBuilder.DropTable(
                name: "stickyroles");

            migrationBuilder.DropTable(
                name: "todos");

            migrationBuilder.DropTable(
                name: "giveawaymodel");

            migrationBuilder.DropTable(
                name: "todosarchive");

            migrationBuilder.DropIndex(
                name: "ix_clubs_name",
                table: "clubs");

            migrationBuilder.DropColumn(
                name: "command",
                table: "shopentry");

            migrationBuilder.DropColumn(
                name: "type",
                table: "reminders");

            migrationBuilder.DropColumn(
                name: "stickyroles",
                table: "guildconfigs");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "clubs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_clubs_name",
                table: "clubs",
                column: "name");

            migrationBuilder.CreateTable(
                name: "nsfwblacklistedtags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    tag = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nsfwblacklistedtags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "poll",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    question = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_poll", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pollanswer",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    index = table.Column<int>(type: "integer", nullable: false),
                    pollid = table.Column<int>(type: "integer", nullable: true),
                    text = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pollanswer", x => x.id);
                    table.ForeignKey(
                        name: "fk_pollanswer_poll_pollid",
                        column: x => x.pollid,
                        principalTable: "poll",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pollvote",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    pollid = table.Column<int>(type: "integer", nullable: true),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    voteindex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pollvote", x => x.id);
                    table.ForeignKey(
                        name: "fk_pollvote_poll_pollid",
                        column: x => x.pollid,
                        principalTable: "poll",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_nsfwblacklistedtags_guildid",
                table: "nsfwblacklistedtags",
                column: "guildid");

            migrationBuilder.CreateIndex(
                name: "ix_poll_guildid",
                table: "poll",
                column: "guildid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pollanswer_pollid",
                table: "pollanswer",
                column: "pollid");

            migrationBuilder.CreateIndex(
                name: "ix_pollvote_pollid",
                table: "pollvote",
                column: "pollid");
        }
    }
}
