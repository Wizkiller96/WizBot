using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations.Mysql
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
                oldType: "int");

            migrationBuilder.AlterColumn<long>(
                name: "awardedxp",
                table: "userxpstats",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<long>(
                name: "amountrewardedthismonth",
                table: "rewardedusers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "verboseerrors",
                table: "guildconfigs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<long>(
                name: "totalxp",
                table: "discorduser",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.CreateTable(
                name: "patronquotas",
                columns: table => new
                {
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    featuretype = table.Column<int>(type: "int", nullable: false),
                    feature = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hourlycount = table.Column<uint>(type: "int unsigned", nullable: false),
                    dailycount = table.Column<uint>(type: "int unsigned", nullable: false),
                    monthlycount = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patronquotas", x => new { x.userid, x.featuretype, x.feature });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "patrons",
                columns: table => new
                {
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    uniqueplatformuserid = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    amountcents = table.Column<int>(type: "int", nullable: false),
                    lastcharge = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    validthru = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patrons", x => x.userid);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "awardedxp",
                table: "userxpstats",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "amountrewardedthismonth",
                table: "rewardedusers",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<bool>(
                name: "verboseerrors",
                table: "guildconfigs",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<int>(
                name: "totalxp",
                table: "discorduser",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 0L);
        }
    }
}
