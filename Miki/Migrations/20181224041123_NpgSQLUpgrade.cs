using Microsoft.EntityFrameworkCore.Migrations;

namespace Miki.Core.Migrations
{
    public partial class NpgSQLUpgrade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                newName: "UserId",
                schema: "dbo",
                table: "Achievements");

            migrationBuilder.DropForeignKey(
                name: "FK_Achievements_Users_UserId",
                schema: "dbo",
                table: "Achievements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Achievements",
                schema: "dbo",
                table: "Achievements");

            migrationBuilder.DropIndex(
                name: "IX_Achievements_UserId",
                schema: "dbo",
                table: "Achievements");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                schema: "dbo",
                table: "Achievements",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Achievements",
                schema: "dbo",
                table: "Achievements",
                columns: new[] { "UserId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_Achievements_Users_UserId",
                schema: "dbo",
                table: "Achievements",
                column: "UserId",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Achievements_Users_UserId",
                schema: "dbo",
                table: "Achievements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Achievements",
                schema: "dbo",
                table: "Achievements");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                schema: "dbo",
                table: "Achievements",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.RenameColumn(
                name: "UserId",
                newName: "Id",
                schema: "dbo",
                table: "Achievements");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Achievements",
                schema: "dbo",
                table: "Achievements",
                columns: new[] { "Id", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_UserId",
                schema: "dbo",
                table: "Achievements",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Achievements_Users_UserId",
                schema: "dbo",
                table: "Achievements",
                column: "Id",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
