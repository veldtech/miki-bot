using Microsoft.EntityFrameworkCore.Migrations;

namespace Miki.Core.Migrations
{
    public partial class state_rework : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChannelId",
                schema: "dbo",
                table: "ModuleStates",
                newName: "GuildId");

            migrationBuilder.RenameColumn(
                name: "ModuleName",
                schema: "dbo",
                table: "ModuleStates",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "CommandName",
                schema: "dbo",
                table: "CommandStates",
                newName: "Name");

            migrationBuilder.AddColumn<long>(
                name: "GuildId",
                schema: "dbo",
                table: "CommandStates",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuildId",
                schema: "dbo",
                table: "CommandStates");

            migrationBuilder.RenameColumn(
                name: "GuildId",
                schema: "dbo",
                table: "ModuleStates",
                newName: "ChannelId");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "dbo",
                table: "ModuleStates",
                newName: "ModuleName");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "dbo",
                table: "CommandStates",
                newName: "CommandName");
        }
    }
}
