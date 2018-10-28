using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Miki.Core.Migrations
{
	public partial class marriage_fix : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_UsersMarriedTo_Users_UserId",
				schema: "dbo",
				table: "UsersMarriedTo");

			migrationBuilder.DropPrimaryKey(
				name: "PK_UsersMarriedTo",
				schema: "dbo",
				table: "UsersMarriedTo");

			migrationBuilder.DropIndex(
				name: "IX_UsersMarriedTo_MarriageId",
				schema: "dbo",
				table: "UsersMarriedTo");

			migrationBuilder.DropColumn(
				name: "Asker",
				schema: "dbo",
				table: "UsersMarriedTo");

			migrationBuilder.RenameColumn(
				name: "UserId",
				schema: "dbo",
				table: "UsersMarriedTo",
				newName: "ReceiverId");

			migrationBuilder.AddColumn<long>(
				name: "AskerId",
				schema: "dbo",
				table: "UsersMarriedTo",
				nullable: false,
				defaultValue: 0L);

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 14, 11, 9, 18, 393, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 12, 4, 46, 34, 305, DateTimeKind.Local));

			migrationBuilder.AddPrimaryKey(
				name: "PK_UsersMarriedTo",
				schema: "dbo",
				table: "UsersMarriedTo",
				columns: new[] { "AskerId", "ReceiverId" });

			migrationBuilder.CreateIndex(
				name: "IX_UsersMarriedTo_MarriageId",
				schema: "dbo",
				table: "UsersMarriedTo",
				column: "MarriageId",
				unique: true);

			migrationBuilder.AddForeignKey(
				name: "FK_UsersMarriedTo_Users_AskerId",
				schema: "dbo",
				table: "UsersMarriedTo",
				column: "AskerId",
				principalSchema: "dbo",
				principalTable: "Users",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_UsersMarriedTo_Users_AskerId",
				schema: "dbo",
				table: "UsersMarriedTo");

			migrationBuilder.DropPrimaryKey(
				name: "PK_UsersMarriedTo",
				schema: "dbo",
				table: "UsersMarriedTo");

			migrationBuilder.DropIndex(
				name: "IX_UsersMarriedTo_MarriageId",
				schema: "dbo",
				table: "UsersMarriedTo");

			migrationBuilder.DropColumn(
				name: "AskerId",
				schema: "dbo",
				table: "UsersMarriedTo");

			migrationBuilder.RenameColumn(
				name: "ReceiverId",
				schema: "dbo",
				table: "UsersMarriedTo",
				newName: "UserId");

			migrationBuilder.AddColumn<bool>(
				name: "Asker",
				schema: "dbo",
				table: "UsersMarriedTo",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AlterColumn<DateTime>(
				name: "ValidUntil",
				schema: "dbo",
				table: "IsDonator",
				nullable: false,
				defaultValue: new DateTime(2018, 3, 12, 4, 46, 34, 305, DateTimeKind.Local),
				oldClrType: typeof(DateTime),
				oldDefaultValue: new DateTime(2018, 3, 14, 11, 9, 18, 393, DateTimeKind.Local));

			migrationBuilder.AddPrimaryKey(
				name: "PK_UsersMarriedTo",
				schema: "dbo",
				table: "UsersMarriedTo",
				columns: new[] { "UserId", "MarriageId" });

			migrationBuilder.CreateIndex(
				name: "IX_UsersMarriedTo_MarriageId",
				schema: "dbo",
				table: "UsersMarriedTo",
				column: "MarriageId");

			migrationBuilder.AddForeignKey(
				name: "FK_UsersMarriedTo_Users_UserId",
				schema: "dbo",
				table: "UsersMarriedTo",
				column: "UserId",
				principalSchema: "dbo",
				principalTable: "Users",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}
	}
}