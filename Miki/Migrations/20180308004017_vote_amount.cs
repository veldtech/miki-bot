using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class vote_amount : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<uint>(
				name: "DblVotes",
				schema: "dbo",
				table: "Users",
				nullable: false,
				defaultValue: 0u);

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 7, 1, 40, 17, 540, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 4, 4, 24, 38, 672, DateTimeKind.Local));
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "DblVotes",
				schema: "dbo",
				table: "Users");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 4, 4, 24, 38, 672, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 7, 1, 40, 17, 540, DateTimeKind.Local));
		}
	}
}