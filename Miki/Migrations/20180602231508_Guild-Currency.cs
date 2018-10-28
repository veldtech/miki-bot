using Microsoft.EntityFrameworkCore.Migrations;

namespace Miki.Core.Migrations
{
	public partial class GuildCurrency : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<int>(
				name: "CurrentBalance",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<long>(
				name: "Currency",
				schema: "dbo",
				table: "GuildUsers",
				nullable: false,
				defaultValue: 0L);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "CurrentBalance",
				schema: "dbo",
				table: "IsDonator");

			migrationBuilder.DropColumn(
				name: "Currency",
				schema: "dbo",
				table: "GuildUsers");
		}
	}
}