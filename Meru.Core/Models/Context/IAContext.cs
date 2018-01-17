using Microsoft.EntityFrameworkCore;

namespace IA.Models.Context
{
    internal class IAContext : DbContext
    {
        public DbSet<Identifier> Identifiers { get; set; }
        public DbSet<CommandState> CommandStates { get; set; }
        public DbSet<ModuleState> ModuleStates { get; set; }

        public IAContext()
        {
        }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseNpgsql(Bot.instance.clientInformation.DatabaseConnectionString);
			base.OnConfiguring(optionsBuilder);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<CommandState>()
				.HasKey(c => new { c.CommandName, c.ChannelId });

			modelBuilder.Entity<Identifier>()
				.HasKey(c => new { c.GuildId, c.DefaultValue });

			modelBuilder.Entity<ModuleState>()
				.HasKey(c => new { c.ModuleName, c.ChannelId});

			modelBuilder.HasDefaultSchema("dbo");
		}
	}
}