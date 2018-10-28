using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class bg_fixes : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropPrimaryKey(
				name: "PK_BackgroundsOwned",
				schema: "dbo",
				table: "BackgroundsOwned");

			migrationBuilder.DropIndex(
				name: "IX_BackgroundsOwned_BackgroundId",
				schema: "dbo",
				table: "BackgroundsOwned");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 4, 30, 20, 2, 41, 34, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 4, 16, 16, 43, 10, 204, DateTimeKind.Local));

			migrationBuilder.AlterColumn<long>(
				name: "UserId",
				schema: "dbo",
				table: "BackgroundsOwned",
				nullable: false,
				oldClrType: typeof(long));

			migrationBuilder.AddPrimaryKey(
				name: "PK_BackgroundsOwned",
				schema: "dbo",
				table: "BackgroundsOwned",
				columns: new[] { "UserId", "BackgroundId" });
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropPrimaryKey(
				name: "PK_BackgroundsOwned",
				schema: "dbo",
				table: "BackgroundsOwned");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 4, 16, 16, 43, 10, 204, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 4, 30, 20, 2, 41, 34, DateTimeKind.Local));

			migrationBuilder.AlterColumn<long>(
				name: "UserId",
				schema: "dbo",
				table: "BackgroundsOwned",
				nullable: false,
				oldClrType: typeof(long));

			migrationBuilder.AddPrimaryKey(
				name: "PK_BackgroundsOwned",
				schema: "dbo",
				table: "BackgroundsOwned",
				column: "UserId");

			migrationBuilder.CreateIndex(
				name: "IX_BackgroundsOwned_BackgroundId",
				schema: "dbo",
				table: "BackgroundsOwned",
				column: "BackgroundId",
				unique: true);
		}
	}
}