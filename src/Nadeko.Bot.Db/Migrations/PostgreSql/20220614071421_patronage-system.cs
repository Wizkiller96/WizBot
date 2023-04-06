using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations.PostgreSql
{
    public partial class patronagesystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "patreonuserid",
                table: "rewardedusers",
                newName: "platformuserid");

            migrationBuilder.RenameIndex(
                name: "ix_rewardedusers_patreonuserid",
                table: "rewardedusers",
                newName: "ix_rewardedusers_platformuserid");

            migrationBuilder.AlterColumn<long>(
                name: "xp",
                table: "userxpstats",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "awardedxp",
                table: "userxpstats",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "amountrewardedthismonth",
                table: "rewardedusers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<bool>(
                name: "verboseerrors",
                table: "guildconfigs",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<long>(
                name: "totalxp",
                table: "discorduser",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.CreateTable(
                name: "patronquotas",
                columns: table => new
                {
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    featuretype = table.Column<int>(type: "integer", nullable: false),
                    feature = table.Column<string>(type: "text", nullable: false),
                    hourlycount = table.Column<long>(type: "bigint", nullable: false),
                    dailycount = table.Column<long>(type: "bigint", nullable: false),
                    monthlycount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patronquotas", x => new { x.userid, x.featuretype, x.feature });
                });

            migrationBuilder.CreateTable(
                name: "patrons",
                columns: table => new
                {
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    uniqueplatformuserid = table.Column<string>(type: "text", nullable: true),
                    amountcents = table.Column<int>(type: "integer", nullable: false),
                    lastcharge = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    validthru = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patrons", x => x.userid);
                });

            migrationBuilder.CreateIndex(
                name: "ix_patronquotas_userid",
                table: "patronquotas",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_patrons_uniqueplatformuserid",
                table: "patrons",
                column: "uniqueplatformuserid",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patronquotas");

            migrationBuilder.DropTable(
                name: "patrons");

            migrationBuilder.RenameColumn(
                name: "platformuserid",
                table: "rewardedusers",
                newName: "patreonuserid");

            migrationBuilder.RenameIndex(
                name: "ix_rewardedusers_platformuserid",
                table: "rewardedusers",
                newName: "ix_rewardedusers_patreonuserid");

            migrationBuilder.AlterColumn<int>(
                name: "xp",
                table: "userxpstats",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "awardedxp",
                table: "userxpstats",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "amountrewardedthismonth",
                table: "rewardedusers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<bool>(
                name: "verboseerrors",
                table: "guildconfigs",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<int>(
                name: "totalxp",
                table: "discorduser",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 0L);
        }
    }
}
