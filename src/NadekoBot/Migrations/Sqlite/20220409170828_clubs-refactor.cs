using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    public partial class clubsrefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE Clubs
SET Name = Name || '#' || Discrim
WHERE Discrim <> 1;

UPDATE Clubs as co
SET Name =
	CASE (select count(*) from Clubs as ci where co.Name == ci.Name) = 1
		WHEN true
			THEN Name
		ELSE
			Name || '#' || Discrim
    END
 WHERE Discrim = 1;");
            
            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_DiscordUser_OwnerId",
                table: "Clubs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Clubs_Name_Discrim",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "Discrim",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "MinimumLevelReq",
                table: "Clubs");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLevelUp",
                table: "UserXpStats",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValue: new DateTime(2017, 9, 21, 20, 53, 13, 307, DateTimeKind.Local));

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "Clubs",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Clubs_Name",
                table: "Clubs",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_DiscordUser_OwnerId",
                table: "Clubs",
                column: "OwnerId",
                principalTable: "DiscordUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_DiscordUser_OwnerId",
                table: "Clubs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Clubs_Name",
                table: "Clubs");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLevelUp",
                table: "UserXpStats",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2017, 9, 21, 20, 53, 13, 307, DateTimeKind.Local),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "datetime('now')");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "Clubs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Discrim",
                table: "Clubs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinimumLevelReq",
                table: "Clubs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Clubs_Name_Discrim",
                table: "Clubs",
                columns: new[] { "Name", "Discrim" });

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_DiscordUser_OwnerId",
                table: "Clubs",
                column: "OwnerId",
                principalTable: "DiscordUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
