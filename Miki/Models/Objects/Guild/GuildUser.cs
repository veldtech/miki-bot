using Discord.WebSocket;
using IA;
using IA.SDK.Interfaces;
using Miki.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models
{
    [Table("GuildUsers")]
    public class GuildUser : IDatabaseUser
    {
        [Key, Column("EntityId", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        public string Name { get; set; }

        public int Experience { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long RivalId { get; set; }

        public int UserCount { get; set; }

        public DateTime LastRivalRenewed { get; set; }
        public DateTime LastRewardClaimed { get; set; }

        public static async Task Create(IDiscordGuild g)
        {
            using (var context = new MikiContext())
            {
                context.GuildUsers.Add(
                    new GuildUser()
                    {
                        Id = g.Id.ToDbLong(),
                        Name = g.Name,
                        Experience = 0,
                        RivalId = 0,
                        UserCount = g.UserCount
                    });

                await context.SaveChangesAsync();
            }
        }

        // TODO: rework this
        public int CalculateLevel(int exp)
        {
            int experience = exp;
            int Level = 0;
            int output = 0;
            while (experience >= output)
            {
                output = CalculateNextLevelIteration(output, Level);
                Level++;
            }
            return Level;
        }
        public int CalculateMaxExperience(int localExp)
        {
            int experience = localExp;
            int Level = 0;
            int output = 0;
            while (experience >= output)
            {
                output = CalculateNextLevelIteration(output, Level);
                Level++;
            }
            return output;
        }
        private int CalculateNextLevelIteration(int output, int level)
        {
            return 10 + (output + (level * 20));
        }

        public int GetGlobalRank()
        {
            using (var context = new MikiContext())
            {
                int x = context.Database.SqlQuery<int>("Select COUNT(*) as rank from GuildUsers where GuildUsers.Experience >= @p0", Experience).First();
                return x;
            }
        }

        public async Task<GuildUser> GetRival()
        {
            if(RivalId == 0)
            {
                return null;
            }

            using (MikiContext context = new MikiContext())
            {
                return await context.GuildUsers.FindAsync(RivalId);
            }
        }
    }
}
