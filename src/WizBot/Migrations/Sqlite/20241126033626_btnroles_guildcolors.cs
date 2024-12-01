using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    /// <inheritdoc />
    public partial class btnroles_guildcolors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ButtonRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ButtonId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Emote = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Exclusive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ButtonRole", x => x.Id);
                    table.UniqueConstraint("AK_ButtonRole_RoleId_MessageId", x => new { x.RoleId, x.MessageId });
                });

            migrationBuilder.CreateTable(
                name: "GuildColors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    OkColor = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true),
                    ErrorColor = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true),
                    PendingColor = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildColors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ButtonRole_GuildId",
                table: "ButtonRole",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildColors_GuildId",
                table: "GuildColors",
                column: "GuildId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ButtonRole");

            migrationBuilder.DropTable(
                name: "GuildColors");
        }
    }
}