using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class type_fixes : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Achievements_Users_Id",
				schema: "dbo",
				table: "Achievements");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 10, 4, 55, 47, 945, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 9, 12, 59, 29, 773, DateTimeKind.Local));

			migrationBuilder.AddColumn<long>(
				name: "UserId",
				schema: "dbo",
				table: "Achievements",
				nullable: true);

			migrationBuilder.CreateIndex(
				name: "IX_Achievements_UserId",
				schema: "dbo",
				table: "Achievements",
				column: "UserId");

			migrationBuilder.AddForeignKey(
				name: "FK_Achievements_Users_UserId",
				schema: "dbo",
				table: "Achievements",
				column: "UserId",
				principalSchema: "dbo",
				principalTable: "Users",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Achievements_Users_UserId",
				schema: "dbo",
				table: "Achievements");

			migrationBuilder.DropIndex(
				name: "IX_Achievements_UserId",
				schema: "dbo",
				table: "Achievements");

			migrationBuilder.DropColumn(
				name: "UserId",
				schema: "dbo",
				table: "Achievements");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 9, 12, 59, 29, 773, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 10, 4, 55, 47, 945, DateTimeKind.Local));

			migrationBuilder.AddForeignKey(
				name: "FK_Achievements_Users_Id",
				schema: "dbo",
				table: "Achievements",
				column: "Id",
				principalSchema: "dbo",
				principalTable: "Users",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}
	}
}