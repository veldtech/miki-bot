using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

            experience = context.Experience.Add(experience);
            await context.SaveChangesAsync();

            return experience;
        }
    }
}