using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    }
}