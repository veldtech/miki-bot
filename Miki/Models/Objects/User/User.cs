using IA;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IA.SDK.Interfaces;
using Miki.Accounts;
using System.ComponentModel;

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

        public void AddCurrency(MikiContext context, User fromUser, int amount)
        {
            Currency += amount;

            if (fromUser != null)
            {

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
                LastDailyTime = DateTime.Parse("1/1/1753 12:00:00 AM"),
                MarriageSlots = 5,
                Name = e.Author.Username,
                Title = "",
                Total_Commands = 0,
                Total_Experience = 0
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

        public int GetGlobalRank()
        {
            using (var context = new MikiContext())
            {
                int x = context.Database.SqlQuery<int>("Select COUNT(*) as rank from Users where Users.Total_Experience >= @p0", Total_Experience).First();
                return x;
            }
        }
        public async Task<int> GetLocalRank(ulong guildId)
        {
            using (var context = new MikiContext())
            {
                LocalExperience l = await context.Experience.FindAsync(guildId.ToDbLong(), Id);

                if(l == null)
                {
                    return -1;
                }

                int x = context.Database.SqlQuery<int>("Select COUNT(*) as rank from LocalExperience where LocalExperience.ServerId = @p0 AND LocalExperience.Experience >= @p1", guildId.ToDbLong(), l.Experience).First();
                return x;
            }
        }

        public bool IsDonator(MikiContext context)
        {
            return context.Achievements.Find(Id, "donator") != null;
        }
    }
}
