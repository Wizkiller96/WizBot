using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations.PostgreSql
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
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    featuretype = table.Column<int>(type: "integer", nullable: false),
                    feature = table.Column<string>(type: "text", nullable: false),
                    dailycount = table.Column<long>(type: "bigint", nullable: false),
                    hourlycount = table.Column<long>(type: "bigint", nullable: false),
                    monthlycount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patronquotas", x => new { x.userid, x.featuretype, x.feature });
                });

            migrationBuilder.CreateIndex(
                name: "ix_patronquotas_userid",
                table: "patronquotas",
                column: "userid");
        }
    }
}