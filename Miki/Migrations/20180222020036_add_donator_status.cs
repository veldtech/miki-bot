using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class add_donator_status : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "DonatorKey",
				schema: "dbo",
				columns: table => new
				{
					Key = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
					StatusTime = table.Column<TimeSpan>(nullable: false, defaultValueSql: "interval '31 days'")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_DonatorKey", x => x.Key);
				});

			migrationBuilder.CreateTable(
				name: "IsDonator",
				schema: "dbo",
				columns: table => new
				{
					UserId = table.Column<long>(nullable: false),
					DaysDonated = table.Column<int>(nullable: false, defaultValue: 0),
					ValidUntil = table.Column<DateTime>(nullable: false, defaultValue: new DateTime(2018, 2, 21, 3, 0, 36, 54, DateTimeKind.Local))
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_IsDonator", x => x.UserId);
				});
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "DonatorKey",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "IsDonator",
				schema: "dbo");
		}
	}
}