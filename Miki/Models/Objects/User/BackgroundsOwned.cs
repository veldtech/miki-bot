using Miki.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models.Objects.User
{
	public class BackgroundsOwned
	{
		public long UserId { get; set; }
		public int BackgroundId { get; set; }

		public static Task<BackgroundsOwned> GetAsync(ulong userId, int backgroundId, MikiContext context)
			=> GetAsync(userId.ToDbLong(), backgroundId, context);
		public static async Task<BackgroundsOwned> GetAsync(long userId, int backgroundId, MikiContext context)
		{
			var bg = await context.BackgroundsOwned.FindAsync(userId, backgroundId);
			if (bg == null)
			{
				return (await context.BackgroundsOwned.AddAsync(new BackgroundsOwned() { UserId = userId, BackgroundId = backgroundId })).Entity;
			}
			return bg;
		}
	}
}