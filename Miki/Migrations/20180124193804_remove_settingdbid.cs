using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class remove_settingdbid : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropUniqueConstraint(
				name: "AK_Settings_EntityId_EntityType_SettingId",
				schema: "dbo",
				table: "Settings");

			migrationBuilder.DropColumn(
				name: "EntityType",
				schema: "dbo",
				table: "Settings");

			migrationBuilder.DropColumn(
				name: "LastExperienceTime",
				schema: "dbo",
				table: "LocalExperience");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<int>(
				name: "EntityType",
				schema: "dbo",
				table: "Settings",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<DateTime>(
				name: "LastExperienceTime",
				schema: "dbo",
				table: "LocalExperience",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddUniqueConstraint(
				name: "AK_Settings_EntityId_EntityType_SettingId",
				schema: "dbo",
				table: "Settings",
				columns: new[] { "EntityId", "EntityType", "SettingId" });
		}
	}
}