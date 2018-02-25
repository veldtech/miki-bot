using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Miki.Core.Migrations
{
    public partial class fix_serial_donator : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ValidUntil",
                schema: "dbo",
                table: "IsDonator",
                nullable: false,
                defaultValue: new DateTime(2018, 2, 23, 21, 35, 10, 170, DateTimeKind.Local),
                oldClrType: typeof(DateTime),
                oldDefaultValue: new DateTime(2018, 2, 21, 3, 0, 36, 54, DateTimeKind.Local));

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                schema: "dbo",
                table: "IsDonator",
                nullable: false,
                oldClrType: typeof(long))
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ValidUntil",
                schema: "dbo",
                table: "IsDonator",
                nullable: false,
                defaultValue: new DateTime(2018, 2, 21, 3, 0, 36, 54, DateTimeKind.Local),
                oldClrType: typeof(DateTime),
                oldDefaultValue: new DateTime(2018, 2, 23, 21, 35, 10, 170, DateTimeKind.Local));

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                schema: "dbo",
                table: "IsDonator",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);
        }
    }
}
