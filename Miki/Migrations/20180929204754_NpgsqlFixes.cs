using Microsoft.EntityFrameworkCore.Migrations;

namespace Miki.Core.Migrations
{
	public partial class NpgsqlFixes : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Achievements_Users_UserId",
				schema: "dbo",
				table: "Achievements");

			migrationBuilder.DropForeignKey(
				name: "FK_Users_Connections_ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users");

			migrationBuilder.DropForeignKey(
				name: "FK_UsersMarriedTo_Users_AskerId",
				schema: "dbo",
				table: "UsersMarriedTo");

			migrationBuilder.DropIndex(
				name: "IX_Users_ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users");

			migrationBuilder.DropPrimaryKey(
				name: "PK_Connections",
				schema: "dbo",
				table: "Connections");

			migrationBuilder.DropIndex(
				name: "IX_Achievements_UserId",
				schema: "dbo",
				table: "Achievements");

			migrationBuilder.DropColumn(
				name: "ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users");

			migrationBuilder.DropColumn(
				name: "UserId",
				schema: "dbo",
				table: "Achievements");

			migrationBuilder.AlterColumn<int>(
				name: "DblVotes",
				schema: "dbo",
				table: "Users",
				nullable: false,
				defaultValue: 0,
				oldClrType: typeof(long));

			migrationBuilder.AlterColumn<long>(
				name: "DiscordUserId",
				schema: "dbo",
				table: "Connections",
				nullable: false,
				oldClrType: typeof(long));

			migrationBuilder.AddColumn<decimal>(
				name: "UserId",
				schema: "dbo",
				table: "Connections",
				nullable: false,
				defaultValue: 0m);

			migrationBuilder.AddColumn<long>(
				name: "UserId1",
				schema: "dbo",
				table: "Connections",
				nullable: true);

			migrationBuilder.AddPrimaryKey(
				name: "PK_Connections",
				schema: "dbo",
				table: "Connections",
				column: "UserId");

			migrationBuilder.CreateTable(
				name: "BankAccounts",
				schema: "dbo",
				columns: table => new
				{
					UserId = table.Column<long>(nullable: false),
					GuildId = table.Column<long>(nullable: false),
					Currency = table.Column<long>(nullable: false),
					TotalDeposited = table.Column<long>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_BankAccounts", x => new { x.GuildId, x.UserId });
				});

			migrationBuilder.CreateTable(
				name: "CommandStates",
				schema: "dbo",
				columns: table => new
				{
					CommandName = table.Column<string>(nullable: false),
					ChannelId = table.Column<long>(nullable: false),
					State = table.Column<bool>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_CommandStates", x => new { x.CommandName, x.ChannelId });
				});

			migrationBuilder.CreateTable(
				name: "Identifiers",
				schema: "dbo",
				columns: table => new
				{
					GuildId = table.Column<long>(nullable: false),
					DefaultValue = table.Column<string>(nullable: false),
					Value = table.Column<string>(nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Identifiers", x => new { x.GuildId, x.DefaultValue });
				});

			migrationBuilder.CreateTable(
				name: "ModuleStates",
				schema: "dbo",
				columns: table => new
				{
					ModuleName = table.Column<string>(nullable: false),
					ChannelId = table.Column<long>(nullable: false),
					State = table.Column<bool>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ModuleStates", x => new { x.ModuleName, x.ChannelId });
				});

			migrationBuilder.CreateIndex(
				name: "IX_Connections_UserId1",
				schema: "dbo",
				table: "Connections",
				column: "UserId1");

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
				name: "FK_Connections_Users_UserId1",
				schema: "dbo",
				table: "Connections",
				column: "UserId1",
				principalSchema: "dbo",
				principalTable: "Users",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Achievements_Users_Id",
				schema: "dbo",
				table: "Achievements");

			migrationBuilder.DropForeignKey(
				name: "FK_Connections_Users_UserId1",
				schema: "dbo",
				table: "Connections");

			migrationBuilder.DropTable(
				name: "BankAccounts",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "CommandStates",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "Identifiers",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "ModuleStates",
				schema: "dbo");

			migrationBuilder.DropPrimaryKey(
				name: "PK_Connections",
				schema: "dbo",
				table: "Connections");

			migrationBuilder.DropIndex(
				name: "IX_Connections_UserId1",
				schema: "dbo",
				table: "Connections");

			migrationBuilder.DropColumn(
				name: "UserId",
				schema: "dbo",
				table: "Connections");

			migrationBuilder.DropColumn(
				name: "UserId1",
				schema: "dbo",
				table: "Connections");

			migrationBuilder.AlterColumn<long>(
				name: "DblVotes",
				schema: "dbo",
				table: "Users",
				nullable: false,
				oldClrType: typeof(int),
				oldDefaultValue: 0);

			migrationBuilder.AddColumn<long>(
				name: "ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users",
				nullable: true);

			migrationBuilder.AlterColumn<long>(
				name: "DiscordUserId",
				schema: "dbo",
				table: "Connections",
				nullable: false,
				oldClrType: typeof(long));

			migrationBuilder.AddColumn<long>(
				name: "UserId",
				schema: "dbo",
				table: "Achievements",
				nullable: true);

			migrationBuilder.AddPrimaryKey(
				name: "PK_Connections",
				schema: "dbo",
				table: "Connections",
				column: "DiscordUserId");

			migrationBuilder.CreateIndex(
				name: "IX_Users_ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users",
				column: "ConnectionsDiscordUserId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Achievements_UserId",
				schema: "dbo",
				table: "Achievements",
				column: "UserId");

			migrationBuilder.AddForeignKey(
				name: "FK_Achievements_Users_UserId",
				schema: "dbo",
				table: "Achievements",
				column: "UserId",
				principalSchema: "dbo",
				principalTable: "Users",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.AddForeignKey(
				name: "FK_Users_Connections_ConnectionsDiscordUserId",
				schema: "dbo",
				table: "Users",
				column: "ConnectionsDiscordUserId",
				principalSchema: "dbo",
				principalTable: "Connections",
				principalColumn: "DiscordUserId",
				onDelete: ReferentialAction.Restrict);

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
	}
}