using Miki.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models.Objects.User
{
    public class ProfileVisuals
    {
		public long UserId { get; set; }
		public int BackgroundId { get; set; } = 0;
		public string ForegroundColor { get; set; } = "#000000";
		public string BackgroundColor { get; set; } = "#000000";

		public static async Task<ProfileVisuals> GetAsync(ulong userId, MikiContext context)
			=> await GetAsync(userId.ToDbLong(), context);
		public static async Task<ProfileVisuals> GetAsync(long userId, MikiContext context)
		{
			ProfileVisuals visuals = await context.ProfileVisuals.FindAsync(userId);

			if (visuals == null)
			{
				visuals = (await context.ProfileVisuals.AddAsync(new ProfileVisuals() { UserId = userId })).Entity;
			}

			return visuals;
		}
	}
}
