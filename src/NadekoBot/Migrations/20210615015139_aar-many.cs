using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class aarmany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AutoAssignRoleIds",
                table: "GuildConfigs",
                nullable: true);
            
            migrationBuilder.Sql(@"UPDATE GuildConfigs
SET AutoAssignRoleIds=CAST(AutoAssignRoleId AS TEXT),
    AutoAssignRoleId = 0
WHERE AutoAssignRoleId is not null and AutoAssignRoleId <> 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoAssignRoleIds",
                table: "GuildConfigs");
        }
    }
}
