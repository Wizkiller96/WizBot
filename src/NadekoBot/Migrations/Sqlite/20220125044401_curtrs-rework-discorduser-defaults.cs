using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    public partial class curtrsreworkdiscorduserdefaults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "CurrencyTransactions",
                newName: "Note");

            migrationBuilder.AlterColumn<int>(
                name: "TotalXp",
                table: "DiscordUser",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "CurrencyAmount",
                table: "DiscordUser",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "Extra",
                table: "CurrencyTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<ulong>(
                name: "OtherId",
                table: "CurrencyTransactions",
                type: "INTEGER",
                nullable: true,
                defaultValueSql: "NULL");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "CurrencyTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Extra",
                table: "CurrencyTransactions");

            migrationBuilder.DropColumn(
                name: "OtherId",
                table: "CurrencyTransactions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CurrencyTransactions");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "CurrencyTransactions",
                newName: "Reason");

            migrationBuilder.AlterColumn<int>(
                name: "TotalXp",
                table: "DiscordUser",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<long>(
                name: "CurrencyAmount",
                table: "DiscordUser",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldDefaultValue: 0L);
        }
    }
}
