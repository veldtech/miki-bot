using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class profile_visuals : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 27, 21, 12, 33, 714, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 14, 11, 9, 18, 393, DateTimeKind.Local));

			migrationBuilder.CreateTable(
				name: "ProfileVisuals",
				schema: "dbo",
				columns: table => new
				{
					UserId = table.Column<long>(nullable: false, defaultValue: 0L),
					BackgroundColor = table.Column<string>(nullable: true, defaultValue: "#000000"),
					BackgroundId = table.Column<int>(nullable: false, defaultValue: 0),
					ForegroundColor = table.Column<string>(nullable: true, defaultValue: "#000000")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ProfileVisuals", x => x.UserId);
				});
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "ProfileVisuals",
				schema: "dbo");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 14, 11, 9, 18, 393, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 27, 21, 12, 33, 714, DateTimeKind.Local));
		}
	}
}