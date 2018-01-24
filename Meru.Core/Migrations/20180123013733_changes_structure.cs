using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Meru.Core.Migrations
{
    public partial class changes_structure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_ModuleStates_ChannelId_ModuleName",
                schema: "dbo",
                table: "ModuleStates");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Identifiers_IdentifierId_GuildId",
                schema: "dbo",
                table: "Identifiers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CommandStates_ChannelId_CommandName",
                schema: "dbo",
                table: "CommandStates");

            migrationBuilder.RenameColumn(
                name: "identifier",
                schema: "dbo",
                table: "Identifiers",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "IdentifierId",
                schema: "dbo",
                table: "Identifiers",
                newName: "DefaultValue");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                schema: "dbo",
                table: "Identifiers",
                newName: "identifier");

            migrationBuilder.RenameColumn(
                name: "DefaultValue",
                schema: "dbo",
                table: "Identifiers",
                newName: "IdentifierId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ModuleStates_ChannelId_ModuleName",
                schema: "dbo",
                table: "ModuleStates",
                columns: new[] { "ChannelId", "ModuleName" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Identifiers_IdentifierId_GuildId",
                schema: "dbo",
                table: "Identifiers",
                columns: new[] { "IdentifierId", "GuildId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CommandStates_ChannelId_CommandName",
                schema: "dbo",
                table: "CommandStates",
                columns: new[] { "ChannelId", "CommandName" });
        }
    }
}
