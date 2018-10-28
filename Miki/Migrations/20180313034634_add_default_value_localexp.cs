using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class add_default_value_localexp : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<int>(
				name: "Experience",
				schema: "dbo",
				table: "LocalExperience",
				nullable: false,
				defaultValue: 0,
				oldClrType: typeof(int));

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 12, 4, 46, 34, 305, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 10, 4, 55, 47, 945, DateTimeKind.Local));
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<int>(
				name: "Experience",
				schema: "dbo",
				table: "LocalExperience",
				nullable: false,
				oldClrType: typeof(int),
				oldDefaultValue: 0);

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 10, 4, 55, 47, 945, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 12, 4, 46, 34, 305, DateTimeKind.Local));
		}
	}
}