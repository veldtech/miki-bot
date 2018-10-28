using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class add_key_count : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 4, 4, 24, 38, 672, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 2, 24, 19, 27, 48, 987, DateTimeKind.Local));

			migrationBuilder.AddColumn<int>(
				name: "KeysRedeemed",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: 0);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "KeysRedeemed",
				schema: "dbo",
				table: "IsDonator");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 2, 24, 19, 27, 48, 987, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 4, 4, 24, 38, 672, DateTimeKind.Local));
		}
	}
}