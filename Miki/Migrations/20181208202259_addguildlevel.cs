using Microsoft.EntityFrameworkCore.Migrations;

namespace Miki.Core.Migrations
{
    public partial class addguildlevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GuildHouseLevel",
                schema: "dbo",
                table: "GuildUsers",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuildHouseLevel",
                schema: "dbo",
                table: "GuildUsers");
        }
    }
}
