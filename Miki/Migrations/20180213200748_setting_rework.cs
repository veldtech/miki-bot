using Microsoft.EntityFrameworkCore.Migrations;

namespace Miki.Core.Migrations
{
	public partial class Setting_rework : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "IsEnabled",
				schema: "dbo",
				table: "Settings");

			migrationBuilder.AddColumn<int>(
				name: "Value",
				schema: "dbo",
				table: "Settings",
				nullable: false,
				defaultValue: 0);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "Value",
				schema: "dbo",
				table: "Settings");

			migrationBuilder.AddColumn<bool>(
				name: "IsEnabled",
				schema: "dbo",
				table: "Settings",
				nullable: false,
				defaultValue: false);
		}
	}
}