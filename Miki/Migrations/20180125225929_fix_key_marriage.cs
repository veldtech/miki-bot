using Microsoft.EntityFrameworkCore.Migrations;

namespace Miki.Core.Migrations
{
	public partial class fix_key_marriage : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropPrimaryKey(
				name: "PK_UsersMarriedTo",
				schema: "dbo",
				table: "UsersMarriedTo");

			migrationBuilder.AddPrimaryKey(
				name: "PK_UsersMarriedTo",
				schema: "dbo",
				table: "UsersMarriedTo",
				columns: new[] { "UserId", "MarriageId" });
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropPrimaryKey(
				name: "PK_UsersMarriedTo",
				schema: "dbo",
				table: "UsersMarriedTo");

			migrationBuilder.AddPrimaryKey(
				name: "PK_UsersMarriedTo",
				schema: "dbo",
				table: "UsersMarriedTo",
				column: "UserId");
		}
	}
}