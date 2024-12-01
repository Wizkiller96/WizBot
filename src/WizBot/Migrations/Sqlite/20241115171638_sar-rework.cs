using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    /// <inheritdoc />
    public partial class sarrework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SarAutoDelete",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                              .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SarAutoDelete", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SarGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                              .Annotation("Sqlite:Autoincrement", true),
                    GroupNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RoleReq = table.Column<ulong>(type: "INTEGER", nullable: true),
                    IsExclusive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SarGroup", x => x.Id);
                    table.UniqueConstraint("AK_SarGroup_GuildId_GroupNumber",
                        x => new
                        {
                            x.GuildId,
                            x.GroupNumber
                        });
                });

            migrationBuilder.CreateTable(
                name: "Sar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                              .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SarGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    LevelReq = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sar", x => x.Id);
                    table.UniqueConstraint("AK_Sar_GuildId_RoleId",
                        x => new
                        {
                            x.GuildId,
                            x.RoleId
                        });
                    table.ForeignKey(
                        name: "FK_Sar_SarGroup_SarGroupId",
                        column: x => x.SarGroupId,
                        principalTable: "SarGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sar_SarGroupId",
                table: "Sar",
                column: "SarGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SarAutoDelete_GuildId",
                table: "SarAutoDelete",
                column: "GuildId",
                unique: true);

            MigrationQueries.MigrateSar(migrationBuilder);

            migrationBuilder.DropTable(
                name: "GroupName");

            migrationBuilder.DropTable(
                name: "SelfAssignableRoles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sar");

            migrationBuilder.DropTable(
                name: "SarAutoDelete");

            migrationBuilder.DropTable(
                name: "SarGroup");

            migrationBuilder.CreateTable(
                name: "GroupName",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                              .Annotation("Sqlite:Autoincrement", true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Number = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupName", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupName_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelfAssignableRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                              .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Group = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    LevelRequirement = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfAssignableRoles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupName_GuildConfigId_Number",
                table: "GroupName",
                columns: new[] { "GuildConfigId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfAssignableRoles_GuildId_RoleId",
                table: "SelfAssignableRoles",
                columns: new[] { "GuildId", "RoleId" },
                unique: true);
        }
    }
}