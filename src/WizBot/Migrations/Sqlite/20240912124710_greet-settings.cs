using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    /// <inheritdoc />
    public partial class greetsettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GreetSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                              .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GreetType = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageText = table.Column<string>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    AutoDeleteTimer = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GreetSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GreetSettings_GuildId_GreetType",
                table: "GreetSettings",
                columns: new[] { "GuildId", "GreetType" },
                unique: true);
            
            MigrationQueries.GreetSettingsCopy(migrationBuilder);

            migrationBuilder.DropColumn(
                name: "AutoDeleteByeMessagesTimer",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "AutoDeleteGreetMessagesTimer",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "BoostMessage",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "BoostMessageChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "BoostMessageDeleteAfter",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "ByeMessageChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "ChannelByeMessageText",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "ChannelGreetMessageText",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "DmGreetMessageText",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "GreetMessageChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "SendBoostMessage",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "SendChannelByeMessage",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "SendChannelGreetMessage",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "SendDmGreetMessage",
                table: "GuildConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GreetSettings");

            migrationBuilder.AddColumn<int>(
                name: "AutoDeleteByeMessagesTimer",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AutoDeleteGreetMessagesTimer",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BoostMessage",
                table: "GuildConfigs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "BoostMessageChannelId",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<int>(
                name: "BoostMessageDeleteAfter",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<ulong>(
                name: "ByeMessageChannelId",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<string>(
                name: "ChannelByeMessageText",
                table: "GuildConfigs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChannelGreetMessageText",
                table: "GuildConfigs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DmGreetMessageText",
                table: "GuildConfigs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "GreetMessageChannelId",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<bool>(
                name: "SendBoostMessage",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SendChannelByeMessage",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SendChannelGreetMessage",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SendDmGreetMessage",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}