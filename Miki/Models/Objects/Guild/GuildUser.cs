using IA;
using IA.SDK.Interfaces;
using Miki.Models.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
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

		[Column("banned")]
		public bool Banned { get; set; }

		#region Config

		public int MinimalExperienceToGetRewards { get; set; }
		public bool VisibleOnLeaderboards { get; set; } = true;

		#endregion Config

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
			int userCount = Bot.instance.Client.GetGuild(id.FromDbLong()).Users.Count;
			int value = await context.Experience
								.Where(x => x.ServerId == id)
								.SumAsync(x => x.Experience);

			var guildUser = new GuildUser();
			guildUser.Name = guild.Name;
			guildUser.Id = id;
			guildUser.Experience = value;
			guildUser.UserCount = 0;
			guildUser.LastRivalRenewed = Utils.MinDbValue;
			guildUser.MinimalExperienceToGetRewards = 100;
			GuildUser outputGuildUser = context.GuildUsers.Add(guildUser);
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