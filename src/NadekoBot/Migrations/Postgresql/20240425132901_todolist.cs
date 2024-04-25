using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NadekoBot.Db.Migrations
{
    /// <inheritdoc />
    public partial class todolist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "todosarchive",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_todosarchive", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "todos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    todo = table.Column<string>(type: "text", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    isdone = table.Column<bool>(type: "boolean", nullable: false),
                    archiveid = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_todos", x => x.id);
                    table.ForeignKey(
                        name: "fk_todos_todosarchive_archiveid",
                        column: x => x.archiveid,
                        principalTable: "todosarchive",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_todos_archiveid",
                table: "todos",
                column: "archiveid");

            migrationBuilder.CreateIndex(
                name: "ix_todos_userid",
                table: "todos",
                column: "userid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "todos");

            migrationBuilder.DropTable(
                name: "todosarchive");
        }
    }
}
