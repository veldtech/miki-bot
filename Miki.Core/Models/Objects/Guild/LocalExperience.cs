using IA;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Models
{
    public class LocalExperience
    {
        [Key, Column("ServerId", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ServerId { get; set; }

        [Key, Column("UserId", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long UserId { get; set; }

        [Column("Experience")]
        public int Experience { get; set; }

        [Column("LastExperienceTime")]
        public DateTime LastExperienceTime { get; set; }

        public static async Task<LocalExperience> CreateAsync(MikiContext context, long ServerId, long userId)
        {
            LocalExperience experience = null;

            experience = new LocalExperience();
            experience.ServerId = ServerId;
            experience.UserId = userId;
            experience.Experience = 0;
            experience.LastExperienceTime = Utils.MinDbValue;

            experience = context.LocalExperience.Add(experience).Entity;
            await context.SaveChangesAsync();

            return experience;
        }
		public static async Task<LocalExperience> GetAsync(MikiContext context, long serverId, long userId)
		{
			return await context.LocalExperience.FindAsync(serverId, userId)
				?? await CreateAsync(context, serverId, userId);
		}

		public async Task<int> GetRank(MikiContext context)
		{
			int x = await context.LocalExperience
				.Where(e => e.ServerId == ServerId && e.Experience > Experience)
				.CountAsync();
			
			return x + 1;
		}
		public static async Task<int> GetRankAsync(MikiContext context, long serverId, long userId)
		{
			return await (await GetAsync(context, serverId, userId)).GetRank(context);
		}
	}	
}