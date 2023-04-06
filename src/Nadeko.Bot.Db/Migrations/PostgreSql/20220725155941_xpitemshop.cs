using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NadekoBot.Migrations.PostgreSql
{
    public partial class xpitemshop : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "xpshopowneditem",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    itemtype = table.Column<int>(type: "integer", nullable: false),
                    isusing = table.Column<bool>(type: "boolean", nullable: false),
                    itemkey = table.Column<string>(type: "text", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_xpshopowneditem", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_xpshopowneditem_userid_itemtype_itemkey",
                table: "xpshopowneditem",
                columns: new[] { "userid", "itemtype", "itemkey" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "xpshopowneditem");
        }
    }
}
