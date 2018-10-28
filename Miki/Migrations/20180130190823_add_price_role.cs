using Microsoft.EntityFrameworkCore.Migrations;

namespace Miki.Core.Migrations
{
	public partial class add_price_role : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<int>(
				name: "Price",
				schema: "dbo",
				table: "LevelRoles",
				nullable: false,
				defaultValue: 0);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "Price",
				schema: "dbo",
				table: "LevelRoles");
		}
	}
}