using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Miki.Core.Migrations
{
	public partial class add_foreign_keys : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropPrimaryKey(
				name: "PK_Marriages",
				schema: "dbo",
				table: "Marriages");

			migrationBuilder.DropColumn(
				name: "Id1",
				schema: "dbo",
				table: "Marriages");

			migrationBuilder.DropColumn(
				name: "Id2",
				schema: "dbo",
				table: "Marriages");

			migrationBuilder.AddColumn<long>(
				name: "ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users",
				nullable: true);

			migrationBuilder.AddColumn<long>(
				name: "MarriageId",
				schema: "dbo",
				table: "Marriages",
				nullable: false)
				.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

			migrationBuilder.AlterColumn<int>(
				name: "RequiredLevel",
				schema: "dbo",
				table: "LevelRoles",
				nullable: false,
				defaultValue: 0,
				oldClrType: typeof(int));

			migrationBuilder.AddColumn<bool>(
				name: "Automatic",
				schema: "dbo",
				table: "LevelRoles",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AddColumn<bool>(
				name: "Optable",
				schema: "dbo",
				table: "LevelRoles",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AddColumn<long>(
				name: "RequiredRole",
				schema: "dbo",
				table: "LevelRoles",
				nullable: false,
				defaultValue: 0L);

			migrationBuilder.AddPrimaryKey(
				name: "PK_Marriages",
				schema: "dbo",
				table: "Marriages",
				column: "MarriageId");

			migrationBuilder.CreateTable(
				name: "Connections",
				schema: "dbo",
				columns: table => new
				{
					DiscordUserId = table.Column<long>(nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
					PatreonUserId = table.Column<string>(nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Connections", x => x.DiscordUserId);
				});

			migrationBuilder.CreateTable(
				name: "UsersMarriedTo",
				schema: "dbo",
				columns: table => new
				{
					UserId = table.Column<long>(nullable: false),
					Asker = table.Column<bool>(nullable: false),
					MarriageId = table.Column<long>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_UsersMarriedTo", x => x.UserId);
					table.ForeignKey(
						name: "FK_UsersMarriedTo_Marriages_MarriageId",
						column: x => x.MarriageId,
						principalSchema: "dbo",
						principalTable: "Marriages",
						principalColumn: "MarriageId",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_UsersMarriedTo_Users_UserId",
						column: x => x.UserId,
						principalSchema: "dbo",
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Users_ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users",
				column: "ConnectionsDiscordUserId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Pastas_CreatorId",
				schema: "dbo",
				table: "Pastas",
				column: "CreatorId");

			migrationBuilder.CreateIndex(
				name: "IX_LocalExperience_UserId",
				schema: "dbo",
				table: "LocalExperience",
				column: "UserId");

			migrationBuilder.CreateIndex(
				name: "IX_UsersMarriedTo_MarriageId",
				schema: "dbo",
				table: "UsersMarriedTo",
				column: "MarriageId");

			migrationBuilder.AddForeignKey(
				name: "FK_Achievements_Users_Id",
				schema: "dbo",
				table: "Achievements",
				column: "Id",
				principalSchema: "dbo",
				principalTable: "Users",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);

			migrationBuilder.AddForeignKey(
				name: "FK_CommandUsages_Users_UserId",
				schema: "dbo",
				table: "CommandUsages",
				column: "UserId",
				principalSchema: "dbo",
				principalTable: "Users",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);

			migrationBuilder.AddForeignKey(
				name: "FK_LocalExperience_Users_UserId",
				schema: "dbo",
				table: "LocalExperience",
				column: "UserId",
				principalSchema: "dbo",
				principalTable: "Users",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);

			migrationBuilder.AddForeignKey(
				name: "FK_Pastas_Users_CreatorId",
				schema: "dbo",
				table: "Pastas",
				column: "CreatorId",
				principalSchema: "dbo",
				principalTable: "Users",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);

			migrationBuilder.AddForeignKey(
				name: "FK_Users_Connections_ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users",
				column: "ConnectionsDiscordUserId",
				principalSchema: "dbo",
				principalTable: "Connections",
				principalColumn: "DiscordUserId",
				onDelete: ReferentialAction.Restrict);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Achievements_Users_Id",
				schema: "dbo",
				table: "Achievements");

			migrationBuilder.DropForeignKey(
				name: "FK_CommandUsages_Users_UserId",
				schema: "dbo",
				table: "CommandUsages");

			migrationBuilder.DropForeignKey(
				name: "FK_LocalExperience_Users_UserId",
				schema: "dbo",
				table: "LocalExperience");

			migrationBuilder.DropForeignKey(
				name: "FK_Pastas_Users_CreatorId",
				schema: "dbo",
				table: "Pastas");

			migrationBuilder.DropForeignKey(
				name: "FK_Users_Connections_ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users");

			migrationBuilder.DropTable(
				name: "Connections",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "UsersMarriedTo",
				schema: "dbo");

			migrationBuilder.DropIndex(
				name: "IX_Users_ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users");

			migrationBuilder.DropIndex(
				name: "IX_Pastas_CreatorId",
				schema: "dbo",
				table: "Pastas");

			migrationBuilder.DropPrimaryKey(
				name: "PK_Marriages",
				schema: "dbo",
				table: "Marriages");

			migrationBuilder.DropIndex(
				name: "IX_LocalExperience_UserId",
				schema: "dbo",
				table: "LocalExperience");

			migrationBuilder.DropColumn(
				name: "ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users");

			migrationBuilder.DropColumn(
				name: "MarriageId",
				schema: "dbo",
				table: "Marriages");

			migrationBuilder.DropColumn(
				name: "Automatic",
				schema: "dbo",
				table: "LevelRoles");

			migrationBuilder.DropColumn(
				name: "Optable",
				schema: "dbo",
				table: "LevelRoles");

			migrationBuilder.DropColumn(
				name: "RequiredRole",
				schema: "dbo",
				table: "LevelRoles");

			migrationBuilder.AddColumn<long>(
				name: "Id1",
				schema: "dbo",
				table: "Marriages",
				nullable: false,
				defaultValue: 0L);

			migrationBuilder.AddColumn<long>(
				name: "Id2",
				schema: "dbo",
				table: "Marriages",
				nullable: false,
				defaultValue: 0L);

			migrationBuilder.AlterColumn<int>(
				name: "RequiredLevel",
				schema: "dbo",
				table: "LevelRoles",
				nullable: false,
				oldClrType: typeof(int),
				oldDefaultValue: 0);

			migrationBuilder.AddPrimaryKey(
				name: "PK_Marriages",
				schema: "dbo",
				table: "Marriages",
				columns: new[] { "Id1", "Id2" });
		}
	}
}