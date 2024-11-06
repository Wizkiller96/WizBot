using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WizBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class nodiscrimandflagtranslate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MigrationQueries.UpdateUsernames(migrationBuilder);
            
            migrationBuilder.DropColumn(
                name: "discriminator",
                table: "discorduser");

            migrationBuilder.CreateTable(
                name: "flagtranslatechannel",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_flagtranslatechannel", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_flagtranslatechannel_guildid_channelid",
                table: "flagtranslatechannel",
                columns: new[] { "guildid", "channelid" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flagtranslatechannel");

            migrationBuilder.AddColumn<string>(
                name: "discriminator",
                table: "discorduser",
                type: "text",
                nullable: true);
        }
    }
}