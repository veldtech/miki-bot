using IA;
using IA.SDK.Interfaces;
using Microsoft.EntityFrameworkCore;
using Miki.Accounts.Achievements;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

		[Column("banned")]
		public bool Banned { get; set; }

        public DateTime LastReputationGiven { get; set; }
        public short ReputationPointsLeft { get; set; }

        public async Task AddCurrencyAsync(int amount, IDiscordMessageChannel channel = null, User fromUser = null)
        {
			if (Banned) return;

            Currency += amount;

            if (channel != null)
            {
                await AchievementManager.Instance.CallTransactionMadeEventAsync(channel, this, fromUser, Currency);
            }
        }

        public static async Task<User> CreateAsync(MikiContext m, IDiscordMessage e)
        {
			return await CreateAsync(m, e.Author);
        }
		public static async Task<User> CreateAsync(MikiContext m, IDiscordUser u)
		{
			User user = new User()
			{
				Id = u.Id.ToDbLong(),
				Currency = 0,
				AvatarUrl = "default",
				HeaderUrl = "default",
				DateCreated = DateTime.Now,
				LastDailyTime = Utils.MinDbValue,
				MarriageSlots = 5,
				Name = u.Username,
				Title = "",
				Total_Commands = 0,
				Total_Experience = 0,
				Reputation = 0,
				LastReputationGiven = Utils.MinDbValue
			};

			m.Users.Add(user);

			return user;
		}
		public static async Task<User> GetAsync(MikiContext context, IDiscordUser u)
		{
			long id = u.Id.ToDbLong();
			return await context.Users.FindAsync(id)
				?? await CreateAsync(context, u);
		}

        public async Task RemoveCurrencyAsync(MikiContext context, User sentTo, int amount)
        {
			if (Banned) return;
			Currency -= amount;
            await context.SaveChangesAsync();
        }

        public static int CalculateLevel(int exp)
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

		public static int CalculateLevelExperience(int level)
		{
			int Level = 0;
			int output = 0;
			do
			{
				output = CalculateNextLevelIteration(output, Level);
				Level++;
			} while (Level < level);

			return output;
		}

        private static int CalculateNextLevelIteration(int output, int level)
        {
            return 10 + (output + (level * 20));
        }

		public async Task<int> GetGlobalReputationRankAsync()
		{
			int x = 0;
			using (var context = new MikiContext())
			{
				x = await context.Users
					.Where(u => u.Reputation > Reputation)
					.CountAsync();
			}
			return x + 1;
		}

		public async Task<int> GetGlobalCommandsRankAsync()
		{
			int x = 0;
			using (var context = new MikiContext())
			{
				x = await context.Users
					.Where(u => u.Total_Commands > Total_Commands)
					.CountAsync();
			}
			return x + 1;
		}

		public async Task<int> GetGlobalMekosRankAsync()
		{
			int x = 0;
			using (var context = new MikiContext())
			{
				x = await context.Users
					.Where(u => u.Currency > Currency)
					.CountAsync();
			}
			return x + 1;
		}

        public async Task<int> GetGlobalRankAsync()
        {
            int x = 0;
            using (var context = new MikiContext())
            {
                x = await context.Users
                    .Where(u => u.Total_Experience > Total_Experience)
                    .CountAsync();
            }
            return x + 1;
        }

        public bool IsDonator(MikiContext context)
        {
            return context.Achievements.Find(Id, "donator") != null;
        }

		public static async Task BanAsync(long id)
		{
			using (var context = new MikiContext())
			{
				User u = await context.Users.FindAsync(id);

				context.Marriages.RemoveRange(
					await context.Marriages.Where(x => x.Id1 == id || x.Id2 == id).ToListAsync()
				);

				context.CommandUsages.RemoveRange(
					await context.CommandUsages.Where(x => x.UserId == id).ToListAsync()
				);

				context.Achievements.RemoveRange(
					await context.Achievements.Where(x => x.Id == id).ToListAsync()
				);

				context.Experience.RemoveRange(
					await context.Experience.Where(x => x.UserId == id).ToListAsync()
				);

				Bot.instance.Events.Ignore(id.FromDbLong());
				u.Banned = true;
				u.Total_Commands = 0;
				u.Total_Experience = 0;
				u.MarriageSlots = 0;
				u.Currency = 0;
				u.Reputation = 0;
				await context.SaveChangesAsync();
			}
		}
    }
}