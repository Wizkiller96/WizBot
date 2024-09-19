using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WizBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class greetsettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "greetsettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                              .Annotation("Npgsql:ValueGenerationStrategy",
                                  NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    greettype = table.Column<int>(type: "integer", nullable: false),
                    messagetext = table.Column<string>(type: "text", nullable: true),
                    isenabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    autodeletetimer = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_greetsettings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_greetsettings_guildid_greettype",
                table: "greetsettings",
                columns: new[] { "guildid", "greettype" },
                unique: true);

            MigrationQueries.GreetSettingsCopy(migrationBuilder);

            migrationBuilder.DropColumn(
                name: "autodeletebyemessagestimer",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "autodeletegreetmessagestimer",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "boostmessage",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "boostmessagechannelid",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "boostmessagedeleteafter",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "byemessagechannelid",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "channelbyemessagetext",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "channelgreetmessagetext",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "dmgreetmessagetext",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "greetmessagechannelid",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "sendboostmessage",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "sendchannelbyemessage",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "sendchannelgreetmessage",
                table: "guildconfigs");

            migrationBuilder.DropColumn(
                name: "senddmgreetmessage",
                table: "guildconfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "greetsettings");

            migrationBuilder.AddColumn<int>(
                name: "autodeletebyemessagestimer",
                table: "guildconfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "autodeletegreetmessagestimer",
                table: "guildconfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "boostmessage",
                table: "guildconfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "boostmessagechannelid",
                table: "guildconfigs",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "boostmessagedeleteafter",
                table: "guildconfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "byemessagechannelid",
                table: "guildconfigs",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "channelbyemessagetext",
                table: "guildconfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "channelgreetmessagetext",
                table: "guildconfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dmgreetmessagetext",
                table: "guildconfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "greetmessagechannelid",
                table: "guildconfigs",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "sendboostmessage",
                table: "guildconfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "sendchannelbyemessage",
                table: "guildconfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "sendchannelgreetmessage",
                table: "guildconfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "senddmgreetmessage",
                table: "guildconfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}