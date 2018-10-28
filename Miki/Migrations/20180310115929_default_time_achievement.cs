using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class default_time_achievement : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 9, 12, 59, 29, 773, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 7, 1, 40, 17, 540, DateTimeKind.Local));

			migrationBuilder.AlterColumn<DateTime>(
				name: "UnlockedAt",
				schema: "dbo",
				table: "Achievements",
				nullable: false,
				defaultValueSql: "now()",
				oldClrType: typeof(DateTime));
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 7, 1, 40, 17, 540, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 9, 12, 59, 29, 773, DateTimeKind.Local));

			migrationBuilder.AlterColumn<DateTime>(
				name: "UnlockedAt",
				schema: "dbo",
				table: "Achievements",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldDefaultValueSql: "now()");
		}
	}
}