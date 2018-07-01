using Miki.Framework;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Miki.Common;
using Miki.Discord.Common;

namespace Miki.Models
{
    public class LocalExperience
    {
        public long ServerId { get; set; }
        public long UserId { get; set; }
        public int Experience { get; set; }

		public User User { get; set; }

		public static async Task<LocalExperience> CreateAsync(MikiContext context, long ServerId, IDiscordUser user)
        {
			long userId = user.Id.ToDbLong();
            LocalExperience experience = null;

            experience = new LocalExperience();
            experience.ServerId = ServerId;
            experience.UserId = userId;
            experience.Experience = 0;

			experience = (await context.LocalExperience.AddAsync(experience)).Entity;

			if (await context.Users.FindAsync(userId) == null)
				await User.CreateAsync(user);

            await context.SaveChangesAsync();

            return experience;
        }
		public static async Task<LocalExperience> GetAsync(MikiContext context, long serverId, IDiscordUser user)
		{
			long userId = user.Id.ToDbLong();
			var localExperience = await context.LocalExperience.FindAsync(serverId, userId);
			if(localExperience == null)
				return await CreateAsync(context, serverId, user);
			return localExperience;
		}

		public async Task<int> GetRank(MikiContext context)
		{
			int x = await context.LocalExperience
				.Where(e => e.ServerId == ServerId && e.Experience > Experience)
				.CountAsync();
			
			return x + 1;
		}
		public static async Task<int> GetRankAsync(MikiContext context, long serverId, IDiscordUser user)
		{
			return await (await GetAsync(context, serverId, user)).GetRank(context);
		}
	}	
}