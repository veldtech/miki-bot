using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class donate_fixes : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValueSql: "now()",
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 4, 30, 20, 2, 41, 34, DateTimeKind.Local));
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 4, 30, 20, 2, 41, 34, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValueSql: "now()");
		}
	}
}