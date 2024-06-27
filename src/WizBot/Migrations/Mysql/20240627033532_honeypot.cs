using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations.Mysql
{
    /// <inheritdoc />
    public partial class honeypot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                                name: "honeypotchannels",
                                columns: table => new
                                {
                                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                                                   .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                                },
                                constraints: table =>
                                {
                                    table.PrimaryKey("pk_honeypotchannels", x => x.guildid);
                                })
                            .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "honeypotchannels");
        }
    }
}