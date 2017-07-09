using Miki.Models.Objects.Guild;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }

        public static MikiContext CreateNoCache()
        {
            MikiContext m = new MikiContext();
            m.Configuration.LazyLoadingEnabled = false;
            return m;
        }
    }
}
