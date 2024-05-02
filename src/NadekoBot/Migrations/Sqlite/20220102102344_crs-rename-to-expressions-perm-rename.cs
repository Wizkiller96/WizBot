using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    public partial class crsrenametoexpressionspermrename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissionv2_GuildConfigs_GuildConfigId",
                table: "Permissionv2");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permissionv2",
                table: "Permissionv2");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CustomReactions",
                table: "CustomReactions");

            migrationBuilder.RenameTable(
                name: "Permissionv2",
                newName: "Permissions");

            migrationBuilder.RenameTable(
                name: "CustomReactions",
                newName: "Expressions");

            migrationBuilder.RenameIndex(
                name: "IX_Permissionv2_GuildConfigId",
                table: "Permissions",
                newName: "IX_Permissions_GuildConfigId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Permissions",
                table: "Permissions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Expressions",
                table: "Expressions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_GuildConfigs_GuildConfigId",
                table: "Permissions",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.Sql(@"UPDATE Permissions
SET SecondaryTargetName='ACTUALEXPRESSIONS'
WHERE SecondaryTargetName='ActualCustomReactions' COLLATE NOCASE;");

            migrationBuilder.Sql(@"UPDATE Permissions
SET SecondaryTargetName='EXPRESSIONS'
WHERE SecondaryTargetName='CustomReactions' COLLATE NOCASE;");

            migrationBuilder.Sql(@"UPDATE Permissions
SET SecondaryTargetName= case lower(SecondaryTargetName) 
    WHEN 'editcustreact' THEN 'expredit'
    WHEN 'delcustreact' THEN 'exprdel'
    WHEN 'listcustreact' THEN 'exprlist'
    WHEN 'addcustreact' THEN 'expradd'
    WHEN 'showcustreact' THEN 'exprshow'
ELSE SecondaryTargetName
END
WHERE lower(SecondaryTargetName) in ('editcustreact', 'delcustreact', 'listcustreact', 'addcustreact', 'showcustreact');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_GuildConfigs_GuildConfigId",
                table: "Permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permissions",
                table: "Permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Expressions",
                table: "Expressions");

            migrationBuilder.RenameTable(
                name: "Permissions",
                newName: "Permissionv2");

            migrationBuilder.RenameTable(
                name: "Expressions",
                newName: "CustomReactions");

            migrationBuilder.RenameIndex(
                name: "IX_Permissions_GuildConfigId",
                table: "Permissionv2",
                newName: "IX_Permissionv2_GuildConfigId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Permissionv2",
                table: "Permissionv2",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CustomReactions",
                table: "CustomReactions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissionv2_GuildConfigs_GuildConfigId",
                table: "Permissionv2",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");
        }
    }
}
