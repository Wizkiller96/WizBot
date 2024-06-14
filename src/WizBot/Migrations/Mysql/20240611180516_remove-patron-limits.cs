using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations.Mysql
{
    /// <inheritdoc />
    public partial class removepatronlimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patronquotas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                                name: "patronquotas",
                                columns: table => new
                                {
                                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                                    featuretype = table.Column<int>(type: "int", nullable: false),
                                    feature = table.Column<string>(type: "varchar(255)", nullable: false)
                                                   .Annotation("MySql:CharSet", "utf8mb4"),
                                    dailycount = table.Column<uint>(type: "int unsigned", nullable: false),
                                    hourlycount = table.Column<uint>(type: "int unsigned", nullable: false),
                                    monthlycount = table.Column<uint>(type: "int unsigned", nullable: false)
                                },
                                constraints: table =>
                                {
                                    table.PrimaryKey("pk_patronquotas", x => new { x.userid, x.featuretype, x.feature });
                                })
                            .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_patronquotas_userid",
                table: "patronquotas",
                column: "userid");
        }
    }
}