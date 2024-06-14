using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    /// <inheritdoc />
    public partial class removepatronlimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatronQuotas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatronQuotas",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    FeatureType = table.Column<int>(type: "INTEGER", nullable: false),
                    Feature = table.Column<string>(type: "TEXT", nullable: false),
                    DailyCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    HourlyCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    MonthlyCount = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatronQuotas", x => new { x.UserId, x.FeatureType, x.Feature });
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatronQuotas_UserId",
                table: "PatronQuotas",
                column: "UserId");
        }
    }
}