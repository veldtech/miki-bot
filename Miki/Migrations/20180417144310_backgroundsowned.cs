using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace Miki.Core.Migrations
{
	public partial class backgroundsowned : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 4, 16, 16, 43, 10, 204, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 27, 21, 12, 33, 714, DateTimeKind.Local));

			migrationBuilder.CreateTable(
				name: "BackgroundsOwned",
				schema: "dbo",
				columns: table => new
				{
					UserId = table.Column<long>(nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
					BackgroundId = table.Column<int>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_BackgroundsOwned", x => x.UserId);
				});

			migrationBuilder.CreateIndex(
				name: "IX_BackgroundsOwned_BackgroundId",
				schema: "dbo",
				table: "BackgroundsOwned",
				column: "BackgroundId",
				unique: true);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "BackgroundsOwned",
				schema: "dbo");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 27, 21, 12, 33, 714, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 4, 16, 16, 43, 10, 204, DateTimeKind.Local));
		}
	}
}