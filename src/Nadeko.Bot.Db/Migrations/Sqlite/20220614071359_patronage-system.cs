using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    public partial class patronagesystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PatreonUserId",
                table: "RewardedUsers",
                newName: "PlatformUserId");

            migrationBuilder.RenameIndex(
                name: "IX_RewardedUsers_PatreonUserId",
                table: "RewardedUsers",
                newName: "IX_RewardedUsers_PlatformUserId");

            migrationBuilder.AlterColumn<bool>(
                name: "VerboseErrors",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "TotalXp",
                table: "DiscordUser",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PatronQuotas",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    FeatureType = table.Column<int>(type: "INTEGER", nullable: false),
                    Feature = table.Column<string>(type: "TEXT", nullable: false),
                    HourlyCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    DailyCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    MonthlyCount = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatronQuotas", x => new { x.UserId, x.FeatureType, x.Feature });
                });

            migrationBuilder.CreateTable(
                name: "Patrons",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UniquePlatformUserId = table.Column<string>(type: "TEXT", nullable: true),
                    AmountCents = table.Column<int>(type: "INTEGER", nullable: false),
                    LastCharge = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidThru = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patrons", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatronQuotas_UserId",
                table: "PatronQuotas",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Patrons_UniquePlatformUserId",
                table: "Patrons",
                column: "UniquePlatformUserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatronQuotas");

            migrationBuilder.DropTable(
                name: "Patrons");

            migrationBuilder.RenameColumn(
                name: "PlatformUserId",
                table: "RewardedUsers",
                newName: "PatreonUserId");

            migrationBuilder.RenameIndex(
                name: "IX_RewardedUsers_PlatformUserId",
                table: "RewardedUsers",
                newName: "IX_RewardedUsers_PatreonUserId");

            migrationBuilder.AlterColumn<bool>(
                name: "VerboseErrors",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<int>(
                name: "TotalXp",
                table: "DiscordUser",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldDefaultValue: 0L);
        }
    }
}
