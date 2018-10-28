using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class rename_days_donated : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.RenameColumn(
				name: "DaysDonated",
				schema: "dbo",
				table: "IsDonator",
				newName: "TotalPaidCents");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 2, 24, 19, 27, 48, 987, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 2, 23, 21, 35, 10, 170, DateTimeKind.Local));
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.RenameColumn(
				name: "TotalPaidCents",
				schema: "dbo",
				table: "IsDonator",
				newName: "DaysDonated");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 2, 23, 21, 35, 10, 170, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 2, 24, 19, 27, 48, 987, DateTimeKind.Local));
		}
	}
}