using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace Miki.Core.Migrations
{
	public partial class initial : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.EnsureSchema(
				name: "dbo");

			migrationBuilder.CreateTable(
				name: "Achievements",
				schema: "dbo",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false),
					Name = table.Column<string>(nullable: false),
					Rank = table.Column<short>(nullable: false),
					UnlockedAt = table.Column<DateTime>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Achievements", x => new { x.Id, x.Name });
				});

			migrationBuilder.CreateTable(
				name: "ChannelLanguage",
				schema: "dbo",
				columns: table => new
				{
					EntityId = table.Column<long>(nullable: false),
					Language = table.Column<string>(nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ChannelLanguage", x => x.EntityId);
				});

			migrationBuilder.CreateTable(
				name: "CommandUsages",
				schema: "dbo",
				columns: table => new
				{
					UserId = table.Column<long>(nullable: false),
					Name = table.Column<string>(nullable: false),
					Amount = table.Column<int>(nullable: false, defaultValue: 1)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_CommandUsages", x => new { x.UserId, x.Name });
				});

			migrationBuilder.CreateTable(
				name: "EventMessages",
				schema: "dbo",
				columns: table => new
				{
					ChannelId = table.Column<long>(nullable: false),
					EventType = table.Column<short>(nullable: false),
					Message = table.Column<string>(nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_EventMessages", x => new { x.ChannelId, x.EventType });
				});

			migrationBuilder.CreateTable(
				name: "GuildUsers",
				schema: "dbo",
				columns: table => new
				{
					EntityId = table.Column<long>(nullable: false),
					banned = table.Column<bool>(nullable: false, defaultValue: false),
					Experience = table.Column<int>(nullable: false),
					LastRivalRenewed = table.Column<DateTime>(nullable: false, defaultValueSql: "now() - INTERVAL '1 day'"),
					MinimalExperienceToGetRewards = table.Column<int>(nullable: false, defaultValue: 100),
					Name = table.Column<string>(nullable: true),
					RivalId = table.Column<long>(nullable: false, defaultValue: 0L),
					UserCount = table.Column<int>(nullable: false, defaultValue: 0),
					VisibleOnLeaderboards = table.Column<bool>(nullable: false, defaultValue: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_GuildUsers", x => x.EntityId);
				});

			migrationBuilder.CreateTable(
				name: "LevelRoles",
				schema: "dbo",
				columns: table => new
				{
					GuildId = table.Column<long>(nullable: false),
					RoleId = table.Column<long>(nullable: false),
					RequiredLevel = table.Column<int>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_LevelRoles", x => new { x.GuildId, x.RoleId });
				});

			migrationBuilder.CreateTable(
				name: "LocalExperience",
				schema: "dbo",
				columns: table => new
				{
					ServerId = table.Column<long>(nullable: false),
					UserId = table.Column<long>(nullable: false),
					Experience = table.Column<int>(nullable: false),
					LastExperienceTime = table.Column<DateTime>(nullable: false, defaultValueSql: "now()")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_LocalExperience", x => new { x.ServerId, x.UserId });
				});

			migrationBuilder.CreateTable(
				name: "Marriages",
				schema: "dbo",
				columns: table => new
				{
					Id1 = table.Column<long>(nullable: false),
					Id2 = table.Column<long>(nullable: false),
					IsProposing = table.Column<bool>(nullable: false),
					TimeOfMarriage = table.Column<DateTime>(nullable: false),
					TimeOfProposal = table.Column<DateTime>(nullable: false, defaultValueSql: "now()")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Marriages", x => new { x.Id1, x.Id2 });
				});

			migrationBuilder.CreateTable(
				name: "Pastas",
				schema: "dbo",
				columns: table => new
				{
					Id = table.Column<string>(nullable: false),
					CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "now()"),
					CreatorId = table.Column<long>(nullable: false),
					Score = table.Column<int>(nullable: false),
					Text = table.Column<string>(nullable: true),
					TimesUsed = table.Column<int>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Pastas", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Settings",
				schema: "dbo",
				columns: table => new
				{
					EntityId = table.Column<long>(nullable: false),
					SettingId = table.Column<int>(nullable: false),
					EntityType = table.Column<int>(nullable: false),
					IsEnabled = table.Column<bool>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Settings", x => new { x.EntityId, x.SettingId });
					table.UniqueConstraint("AK_Settings_EntityId_EntityType_SettingId", x => new { x.EntityId, x.EntityType, x.SettingId });
				});

			migrationBuilder.CreateTable(
				name: "Timers",
				schema: "dbo",
				columns: table => new
				{
					GuildId = table.Column<long>(nullable: false),
					UserId = table.Column<long>(nullable: false),
					Value = table.Column<DateTime>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Timers", x => new { x.GuildId, x.UserId });
				});

			migrationBuilder.CreateTable(
				name: "Users",
				schema: "dbo",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
					AvatarUrl = table.Column<string>(nullable: true, defaultValue: "default"),
					Banned = table.Column<bool>(nullable: false, defaultValue: false),
					Currency = table.Column<int>(nullable: false, defaultValue: 0),
					DateCreated = table.Column<DateTime>(nullable: false, defaultValueSql: "now()"),
					HeaderUrl = table.Column<string>(nullable: true, defaultValue: "default"),
					LastDailyTime = table.Column<DateTime>(nullable: false, defaultValueSql: "now() - interval '1 day'"),
					LastReputationGiven = table.Column<DateTime>(nullable: false, defaultValueSql: "now()"),
					MarriageSlots = table.Column<int>(nullable: false, defaultValue: 0),
					Name = table.Column<string>(nullable: true),
					Reputation = table.Column<int>(nullable: false, defaultValue: 0),
					ReputationPointsLeft = table.Column<short>(nullable: false, defaultValue: (short)3),
					Title = table.Column<string>(nullable: true, defaultValue: ""),
					Total_Commands = table.Column<int>(nullable: false, defaultValue: 0),
					Total_Experience = table.Column<int>(nullable: false, defaultValue: 0)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Users", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Votes",
				schema: "dbo",
				columns: table => new
				{
					Id = table.Column<string>(nullable: false),
					UserId = table.Column<long>(nullable: false),
					PositiveVote = table.Column<bool>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Votes", x => new { x.Id, x.UserId });
				});
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Achievements",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "ChannelLanguage",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "CommandUsages",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "EventMessages",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "GuildUsers",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "LevelRoles",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "LocalExperience",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "Marriages",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "Pastas",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "Settings",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "Timers",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "Users",
				schema: "dbo");

			migrationBuilder.DropTable(
				name: "Votes",
				schema: "dbo");
		}
	}
}