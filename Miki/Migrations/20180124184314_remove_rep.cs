using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class remove_rep : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "LastReputationGiven",
				schema: "dbo",
				table: "Users");

			migrationBuilder.DropColumn(
				name: "ReputationPointsLeft",
				schema: "dbo",
				table: "Users");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<DateTime>(
				name: "LastReputationGiven",
				schema: "dbo",
				table: "Users",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<short>(
				name: "ReputationPointsLeft",
				schema: "dbo",
				table: "Users",
				nullable: false,
				defaultValue: (short)3);
		}
	}
}