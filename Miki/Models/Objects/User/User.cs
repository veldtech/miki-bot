using IA;
using IA.SDK.Interfaces;
using Miki.Accounts.Achievements;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Models
{
    [Table("Users")]
    public class User : IDatabaseEntity
    {
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Column("Title")]
        public string Title { get; set; }

        [Column("Total_Commands")]
        [DefaultValue("0")]
        public int Total_Commands { get; set; }

        [Column("Total_Experience")]
        [DefaultValue("0")]
        public int Total_Experience { get; set; }

        [Column("Currency")]
        [DefaultValue("0")]
        public int Currency { get; set; }

        [Column("MarriageSlots")]
        [DefaultValue("5")]
        public int MarriageSlots { get; set; }

        [Column("AvatarUrl")]
        public string AvatarUrl { get; set; }

        [Column("HeaderUrl")]
        public string HeaderUrl { get; set; }

        [Column("LastDailyTime")]
        public DateTime LastDailyTime { get; set; }

        [Column("DateCreated")]
        [DefaultValue("getutcdate()")]
        public DateTime DateCreated { get; set; }

        [Column("Reputation")]
        public int Reputation { get; set; }

        public DateTime LastReputationGiven { get; set; }
        public short ReputationPointsLeft { get; set; }

        public async Task AddCurrencyAsync(IDiscordMessageChannel context, User fromUser, int amount)
        {
            Currency += amount;

            if (context != null)
            {
                await AchievementManager.Instance.CallTransactionMadeEventAsync(context, this, fromUser, Currency);
            }
        }

        public static User Create(MikiContext context, IDiscordMessage e)
        {
            User user = new User()
            {
                Id = e.Author.Id.ToDbLong(),
                Currency = 0,
                AvatarUrl = "default",
                HeaderUrl = "default",
                DateCreated = DateTime.Now,
                LastDailyTime = Utils.MinDbValue,
                MarriageSlots = 5,
                Name = e.Author.Username,
                Title = "",
                Total_Commands = 0,
                Total_Experience = 0,
                Reputation = 0,
                LastReputationGiven = Utils.MinDbValue
            };
            context.Users.Add(user);
            return user;
        }

        public async Task RemoveCurrencyAsync(MikiContext context, User sentTo, int amount)
        {
            Currency -= amount;
            await context.SaveChangesAsync();
        }

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

        public async Task<int> GetGlobalRank()
        {
            using (var context = new MikiContext())
            {
                int x = await context.Users
                    .Where(u => u.Total_Experience > Total_Experience)
                    .CountAsync();
                return x + 1;
            }
        }

        public async Task<int> GetLocalRank(ulong guildId)
        {
            using (var context = new MikiContext())
            {
                LocalExperience l = await context.Experience.FindAsync(guildId.ToDbLong(), Id);

                if (l == null)
                {
                    return -1;
                }

                long gId = guildId.ToDbLong();
                int x = await context.Experience
                    .Where(e => e.ServerId == gId && e.Experience > l.Experience)
                    .CountAsync();
                return x + 1;
            }
        }

        public bool IsDonator(MikiContext context)
        {
            return context.Achievements.Find(Id, "donator") != null;
        }
    }
}