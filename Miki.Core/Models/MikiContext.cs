using IA;
using IA.FileHandling;
using Microsoft.EntityFrameworkCore;
using Miki.Models.Objects.Guild;
using Newtonsoft.Json;
using System;

namespace Miki.Models
{
    public class MikiContext : DbContext
    {
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<CommandUsage> CommandUsages { get; set; }
		public DbSet<Connection> Connections { get; set; }
        public DbSet<EventMessage> EventMessages { get; set; }
        public DbSet<LocalExperience> LocalExperience { get; set; }
        public DbSet<GuildUser> GuildUsers { get; set; }
        public DbSet<ChannelLanguage> Languages { get; set; }
        public DbSet<LevelRole> LevelRoles { get; set; }
        public DbSet<Marriage> Marriages { get; set; }
        public DbSet<GlobalPasta> Pastas { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Timer> Timers { get; set; }
        public DbSet<User> Users { get; set; }
		public DbSet<UserMarriedTo> UsersMarriedTo { get; set; }
        public DbSet<PastaVote> Votes { get; set; }

        public MikiContext() : base()
        {
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseNpgsql(Global.config.ConnString);
			base.OnConfiguring(optionsBuilder);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			var achievement = modelBuilder.Entity<Achievement>();

			achievement.HasKey(c => new { c.Id, c.Name });

			achievement
				.HasOne(x => x.User)
				.WithMany(x => x.Achievements)
				.HasForeignKey(x => x.Id)
				.HasPrincipalKey(x => x.Id);

			#region Command Usage
			var commandUsage = modelBuilder.Entity<CommandUsage>();

			commandUsage
				.HasKey(c => new { c.UserId, c.Name });

			commandUsage
				.Property(x => x.Amount)
				.HasDefaultValue(1);

			commandUsage
				.HasOne(x => x.User)
				.WithMany(x => x.CommandsUsed)
				.HasForeignKey(x => x.UserId)
				.HasPrincipalKey(x => x.Id);
			#endregion

			#region Connections
			var conn = modelBuilder.Entity<Connection>();

			conn.HasKey(x => x.DiscordUserId);
			
			#endregion

			#region Event Message
			var eventMessage = modelBuilder.Entity<EventMessage>();

			eventMessage
				.HasKey(c => new { c.ChannelId, c.EventType });
			#endregion

			#region Local Experience
			var localExperience = modelBuilder.Entity<LocalExperience>();

			localExperience
				.HasKey(c => new { c.ServerId, c.UserId });

			localExperience
				.HasOne(x => x.User)
				.WithMany(x => x.LocalExperience)
				.HasForeignKey(x => x.UserId)
				.HasPrincipalKey(x => x.Id);

			#endregion

			#region Guild User
			var guildUser = modelBuilder.Entity<GuildUser>();
			guildUser.HasKey(x => x.Id);

			guildUser.Property(x => x.Banned)
				.HasDefaultValue(false);

			guildUser.Property(x => x.VisibleOnLeaderboards)
				.HasDefaultValue(false);

			guildUser.Property(x => x.LastRivalRenewed)
				.HasDefaultValueSql("now() - INTERVAL '1 day'");

			guildUser.Property(x => x.MinimalExperienceToGetRewards)
				.HasDefaultValue(100);

			guildUser.Property(x => x.RivalId)
				.HasDefaultValue(0);

			guildUser.Property(x => x.UserCount)
				.HasDefaultValue(0);
			#endregion

			#region Channel Language
			modelBuilder.Entity<ChannelLanguage>()
				.HasKey(c => c.EntityId);
			#endregion

			#region Level Role
			var role = modelBuilder.Entity<LevelRole>();

			role.HasKey(c => new { c.GuildId, c.RoleId });

			role.Property(x => x.Automatic)
				.HasDefaultValue(false);

			role.Property(x => x.Optable)
				.HasDefaultValue(false);

			role.Property(x => x.RequiredRole)
				.HasDefaultValue(0);

			role.Property(x => x.RequiredLevel)
				.HasDefaultValue(0);

			#endregion

			#region Marriage
			var marriage = modelBuilder.Entity<Marriage>();

			marriage.Property(x => x.MarriageId)
				.ValueGeneratedOnAdd();

			marriage.HasKey(x => x.MarriageId);

			marriage.Property(x => x.TimeOfProposal)
				.HasDefaultValueSql("now()");
			#endregion

			#region Global Pasta
			var globalPasta = modelBuilder.Entity<GlobalPasta>();

			globalPasta
				.HasKey(c => c.Id);

			globalPasta
				.Property(x => x.CreatedAt)
				.HasDefaultValueSql("now()");

			globalPasta.HasOne(x => x.User)
				.WithMany(x => x.Pastas)
				.HasForeignKey(x => x.CreatorId)
				.HasPrincipalKey(x => x.Id);
			#endregion

			#region Setting
			modelBuilder.Entity<Setting>()
				.HasKey(c => new { c.EntityId, c.SettingId });
			#endregion

			#region Timer
			modelBuilder.Entity<Timer>()
				.HasKey(c => new { c.GuildId, c.UserId });
			#endregion

			#region User
			var user = modelBuilder.Entity<User>();

			user.HasKey(c => c.Id);

			user.Property(x => x.Id);

			user.Property(x => x.AvatarUrl)
				.HasDefaultValue("default");

			user.Property(x => x.Banned)
				.HasDefaultValue(false);

			user.Property(x => x.Currency)
				.HasDefaultValue(0);

			user.Property(x => x.DateCreated)
				.HasDefaultValueSql("now()");

			user.Property(x => x.HeaderUrl)
				.HasDefaultValue("default");

			user.Property(x => x.LastDailyTime)
				.HasDefaultValueSql("now() - interval '1 day'");

			user.Property(x => x.MarriageSlots)
				.HasDefaultValue(0);

			user.Property(x => x.Reputation)
				.HasDefaultValue(0);

			user.Property(x => x.Title)
				.HasDefaultValue("");

			user.Property(x => x.Total_Commands)
				.HasDefaultValue(0);

			user.Property(x => x.Total_Experience)
				.HasDefaultValue(0);

			#endregion

			#region UserMarriedTo
			var usermarried = modelBuilder.Entity<UserMarriedTo>();

			usermarried.HasKey(x => new { x.UserId, x.MarriageId });

			usermarried.HasOne(x => x.User)
				.WithMany(x => x.Marriages)
				.HasForeignKey(x => x.UserId)
				.HasPrincipalKey(x => x.Id);

			usermarried.HasOne(x => x.Marriage)
				.WithMany(x => x.Participants)
				.HasForeignKey(x => x.MarriageId)
				.HasPrincipalKey(x => x.MarriageId);
			#endregion

			#region Pasta Vote
			modelBuilder.Entity<PastaVote>()
				.HasKey(c => new { c.Id, c.UserId });
			#endregion

			modelBuilder.HasDefaultSchema("dbo");
		}
	}
}