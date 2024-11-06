using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    /// <inheritdoc />
    public partial class betstats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBetStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                              .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Game = table.Column<int>(type: "INTEGER", nullable: false),
                    WinCount = table.Column<long>(type: "INTEGER", nullable: false),
                    LoseCount = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalBet = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaidOut = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxWin = table.Column<long>(type: "INTEGER", nullable: false),
                    MaxBet = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBetStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBetStats_UserId_Game",
                table: "UserBetStats",
                columns: new[] { "UserId", "Game" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBetStats");
        }
    }
}