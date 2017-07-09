using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models
{
    [Table("LocalExperience")]
    public class LocalExperience
    {
        [Key, Column("ServerId", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long server_id { get; set; }

        [Key, Column("UserId", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long user_id { get; set; }

        [Column("Experience")]
        public int Experience { get; set; }

        [Column("LastExperienceTime")]
        public DateTime LastExperienceTime { get; set; }

        [NotMapped]
        public ulong ServerId
        {
            get
            {
                unchecked
                {
                    return (ulong)server_id;
                }
            }
            set
            {
                unchecked
                {
                    server_id = (long)value;
                }
            }
        }

        [NotMapped]
        public ulong UserId
        {
            get
            {
                unchecked
                {
                    return (ulong)user_id;
                }
            }
            set
            {
                unchecked
                {
                    user_id = (long)value;
                }
            }
        }
    }
}
