using Microsoft.EntityFrameworkCore;
using Miki.Bot.Models.Exceptions;
using Miki.Bot.Models.Repositories;
using Miki.Discord.Common;
using Miki.Exceptions;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Filters;
using Miki.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miki
{
	public static class ModelExtensions
	{
		public static async Task BanAsync(this User user)
		{
			using (var context = new MikiContext())
			{
				MarriageRepository repository = new MarriageRepository(context);

				User u = await context.Users.FindAsync(user.Id);

				await repository.DivorceAllMarriagesAsync(user.Id);
				await repository.DeclineAllProposalsAsync(user.Id);

				context.CommandUsages.RemoveRange(
					await context.CommandUsages.Where(x => x.UserId == user.Id).ToListAsync()
				);

				context.Achievements.RemoveRange(
					await context.Achievements.Where(x => x.UserId == user.Id).ToListAsync()
				);

				context.LocalExperience.RemoveRange(
					await context.LocalExperience.Where(x => x.UserId == user.Id).ToListAsync()
				);

				MikiApp.Instance.GetService<EventSystem>().MessageFilter.Get<UserFilter>().Users.Add(user.Id.FromDbLong());

				u.Banned = true;
				u.Total_Commands = 0;
				u.Total_Experience = 0;
				u.MarriageSlots = 0;
				u.Currency = 0;
				u.Reputation = 0;

				await context.SaveChangesAsync();
			}
		}

		public static async Task<IDiscordRole> GetRoleAsync(this LevelRole role)
		{
			return await MikiApp.Instance.Discord.GetRoleAsync((ulong)role.GuildId, (ulong)role.RoleId);
		}

		public static async Task AddAsync(MikiContext context, string id, string text, long creator)
		{
			if (Regex.IsMatch(text, "(http[s]://)?((discord.gg)|(discordapp.com/invite))/([A-Za-z0-9]+)", RegexOptions.IgnoreCase))
			{
				throw new Exception("You can't add discord invites!");
			}

			GlobalPasta pasta = await context.Pastas.FindAsync(id);

			if (pasta != null)
			{
				throw new DuplicatePastaException(pasta);
			}

			await context.Pastas.AddAsync(new GlobalPasta()
			{
				Id = id,
				Text = text,
				CreatorId = creator,
				CreatedAt = DateTime.Now
			});
		}
	}
}