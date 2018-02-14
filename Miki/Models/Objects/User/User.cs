using Miki.Framework;
using Miki.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Miki.Accounts.Achievements;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public int Total_Commands { get; set; }
        public int Total_Experience { get; set; }
        public int Currency { get; set; }
        public int MarriageSlots { get; set; }
        public string AvatarUrl { get; set; }
		public string HeaderUrl { get; set; }
        public DateTime LastDailyTime { get; set; }

        public DateTime DateCreated { get; set; }
		public int Reputation { get; set; } 
		public bool Banned { get; set; }

		public List<Achievement> Achievements { get; set; }
		public List<UserMarriedTo> Marriages { get; set; }
		public List<CommandUsage> CommandsUsed { get; set; }
		public List<GlobalPasta> Pastas { get; set; }
		public List<LocalExperience> LocalExperience { get; set; }
		public Connection Connections { get; set; }

		public int Level => CalculateLevel(Total_Experience);

        public async Task AddCurrencyAsync(int amount, IDiscordMessageChannel channel = null, User fromUser = null)
        {
			if (Banned) return;

            Currency += amount;

            if (channel != null)
            {
                await AchievementManager.Instance.CallTransactionMadeEventAsync(channel, this, fromUser, Currency);
            }
        }

        public static async Task<User> CreateAsync(IDiscordMessage e)
        {
			return await CreateAsync(e.Author);
        }
		public static async Task<User> CreateAsync(IDiscordUser u)
		{
			using (var context = new MikiContext())
			{
				User user = new User()
				{
					Id = u.Id.ToDbLong(),
					Currency = 0,
					AvatarUrl = "default",
					HeaderUrl = "default",
					LastDailyTime = Utils.MinDbValue,
					MarriageSlots = 5,
					Name = u.Username,
					Title = "",
					Total_Commands = 0,
					Total_Experience = 0,
					Reputation = 0
				};

				LocalExperience exp = new LocalExperience()
				{
					Experience = 0,
					ServerId = u.Guild.Id.ToDbLong(),
					UserId = u.Id.ToDbLong(),
				};

				user.LocalExperience = new List<LocalExperience> { exp };

				await context.Users.AddAsync(user);
				await context.LocalExperience.AddAsync(exp);

				await context.SaveChangesAsync();

				return user;
			}
		}

		public static async Task<User> GetAsync(MikiContext context, IDiscordUser u)
		{
			long id = u.Id.ToDbLong();
			User user = await context.Users.Where(x => x.Id == u.Id.ToDbLong())?
				.Include(x => x.Achievements)
				.Include(x => x.CommandsUsed)
				.Include(x => x.Marriages)
					.ThenInclude(x => x.Marriage)
						.ThenInclude(x => x.Participants)
				.Include(x => x.LocalExperience)
				.Include(x => x.Pastas)
				.Include(x => x.Connections)
				.FirstOrDefaultAsync();

			if(user == null)
			{
				return await CreateAsync(u);
			}
			return user;
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
					await context.UsersMarriedTo.Where(x => x.UserId == id).Select(x => x.Marriage).ToListAsync()
				);

				context.CommandUsages.RemoveRange(
					await context.CommandUsages.Where(x => x.UserId == id).ToListAsync()
				);

				context.Achievements.RemoveRange(
					await context.Achievements.Where(x => x.Id == id).ToListAsync()
				);

				context.LocalExperience.RemoveRange(
					await context.LocalExperience.Where(x => x.UserId == id).ToListAsync()
				);

				Bot.Instance.Events.Ignore(id.FromDbLong());
				u.Banned = true;
				u.Total_Commands = 0;
				u.Total_Experience = 0;
				u.MarriageSlots = 0;
				u.Currency = 0;
				u.Reputation = 0;
				await context.SaveChangesAsync();
			}
		}

		private static int CalculateNextLevelIteration(int output, int level)
		{
			return 10 + (output + (level * 20));
		}
	}

	[ProtoContract]
	public class ReputationObject
	{
		[ProtoMember(1)]
		public DateTime LastReputationGiven { get; set; }

		[ProtoMember(2)]
		public short ReputationPointsLeft { get; set; }
	}

	[ProtoContract]
	public class RealtimeExperienceObject
	{
		[ProtoMember(1)]
		public int Experience { get; set; }

		[ProtoMember(2)]
		public DateTime LastExperienceTime { get; set; }
	}
}