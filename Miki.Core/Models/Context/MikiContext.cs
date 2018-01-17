using IA;
using Microsoft.EntityFrameworkCore;
using Miki.Models.Objects.Guild;
using System;

namespace Miki.Models
{
    public class MikiContext : DbContext
    {
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<CommandUsage> CommandUsages { get; set; }
        public DbSet<EventMessage> EventMessages { get; set; }
        public DbSet<LocalExperience> Experience { get; set; }
        public DbSet<GuildUser> GuildUsers { get; set; }
        public DbSet<ChannelLanguage> Languages { get; set; }
        public DbSet<LevelRole> LevelRoles { get; set; }
        public DbSet<Marriage> Marriages { get; set; }
        public DbSet<GlobalPasta> Pastas { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Timer> Timers { get; set; }
        public DbSet<User> Users { get; set; }
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
			modelBuilder.Entity<Achievement>()
				.HasKey(c => new { c.Id, c.Name });

			modelBuilder.Entity<CommandUsage>()
				.HasKey(c => new { c.UserId, c.Name });

			modelBuilder.Entity<EventMessage>()
				.HasKey(c => new { c.ChannelId, c.EventType });

			modelBuilder.Entity<LocalExperience>()
				.HasKey(c => c.ServerId);

			modelBuilder.Entity<GuildUser>()
				.HasKey(c => c.Id)
				.HasName("GuildUsers");

			modelBuilder.Entity<ChannelLanguage>()
				.HasKey(c => c.EntityId)
				.HasName("ChannelLanguages");

			modelBuilder.Entity<LevelRole>()
				.HasKey(c => new { c.GuildId, c.RoleId });

			modelBuilder.Entity<Marriage>()
				.HasKey(c => new { c.Id1, c.Id2 });

			modelBuilder.Entity<GlobalPasta>()
				.HasKey(c => c.Id);

			modelBuilder.Entity<Setting>()
				.HasKey(c => new { c.EntityId, c.SettingId });

			modelBuilder.Entity<Timer>()
				.HasKey(c => new { c.GuildId, c.UserId });

			modelBuilder.Entity<User>()
				.HasKey(c => c.Id);

			modelBuilder.Entity<PastaVote>()
				.HasKey(c => new { c.Id, c.UserId });

			modelBuilder.HasDefaultSchema("dbo");
		}
	}
}