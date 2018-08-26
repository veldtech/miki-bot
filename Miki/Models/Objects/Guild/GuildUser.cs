using Miki.Framework;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Miki.Exceptions;
using StatsdClient;
using Miki.Discord.Common;

namespace Miki.Models
{
	[Table("GuildUsers")]
	public class GuildUser
	{
		[Key, Column("EntityId", Order = 0)]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public long Id { get; set; }

		public string Name { get; set; }

		public long Currency { get; set; } = 0;

		public int Experience { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public long RivalId { get; set; }

		public int UserCount { get; set; }

		public DateTime LastRivalRenewed { get; set; }

		[Column("banned")]
		public bool Banned { get; set; } = false;

		#region Config

		public int MinimalExperienceToGetRewards { get; set; }
		public bool VisibleOnLeaderboards { get; set; } = true;

		#endregion Config

		public void AddCurrency(int amount, User FromUser)
		{
			if (Banned)
			{
				throw new UserBannedException(FromUser);
			}

			if (amount < 0)
			{
				if (Currency < Math.Abs(amount))
				{
					throw new InsufficientCurrencyException(Currency, Math.Abs(amount));
				}
			}

			DogStatsd.Counter("currency.change", amount);

			Currency += amount;
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
				int rank = context.GuildUsers
					.Where(x => x.Experience > Experience)
					.Count();
				return rank;
			}
		}

		public async Task<GuildUser> GetRival()
		{
			if (RivalId == 0)
			{
				return null;
			}

			using (MikiContext context = new MikiContext())
			{
				return await context.GuildUsers.FindAsync(RivalId);
			}
		}

		public static async Task<GuildUser> CreateAsync(MikiContext context, IDiscordGuild guild)
		{
			long id = guild.Id.ToDbLong();
			int userCount = (await guild.GetMembersAsync()).Length;
			int value = await context.LocalExperience
								.Where(x => x.ServerId == id)
								.SumAsync(x => x.Experience);

			var guildUser = new GuildUser();
			guildUser.Name = guild.Name;
			guildUser.Id = id;
			guildUser.Experience = value;
			guildUser.UserCount = userCount;
			guildUser.LastRivalRenewed = Utils.MinDbValue;
			guildUser.MinimalExperienceToGetRewards = 100;
			GuildUser outputGuildUser = context.GuildUsers.Add(guildUser).Entity;
			await context.SaveChangesAsync();
			return outputGuildUser;
		}

		public static async Task<GuildUser> GetAsync(MikiContext context, IDiscordGuild guild)
		{
			long id = guild.Id.ToDbLong();
			return await context.GuildUsers.FindAsync(id)
				?? await CreateAsync(context, guild);
		}
	}
}