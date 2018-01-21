using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Meru.Core.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "CommandStates",
                schema: "dbo",
                columns: table => new
                {
                    CommandName = table.Column<string>(nullable: false),
                    ChannelId = table.Column<long>(nullable: false),
                    State = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandStates", x => new { x.CommandName, x.ChannelId });
                    table.UniqueConstraint("AK_CommandStates_ChannelId_CommandName", x => new { x.ChannelId, x.CommandName });
                });

            migrationBuilder.CreateTable(
                name: "Identifiers",
                schema: "dbo",
                columns: table => new
                {
                    GuildId = table.Column<long>(nullable: false),
                    IdentifierId = table.Column<string>(nullable: false),
                    identifier = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Identifiers", x => new { x.GuildId, x.IdentifierId });
                    table.UniqueConstraint("AK_Identifiers_IdentifierId_GuildId", x => new { x.IdentifierId, x.GuildId });
                });

            migrationBuilder.CreateTable(
                name: "ModuleStates",
                schema: "dbo",
                columns: table => new
                {
                    ModuleName = table.Column<string>(nullable: false),
                    ChannelId = table.Column<long>(nullable: false),
                    State = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleStates", x => new { x.ModuleName, x.ChannelId });
                    table.UniqueConstraint("AK_ModuleStates_ChannelId_ModuleName", x => new { x.ChannelId, x.ModuleName });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandStates",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Identifiers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ModuleStates",
                schema: "dbo");
        }
    }
}
