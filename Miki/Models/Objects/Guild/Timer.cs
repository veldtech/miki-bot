using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models.Objects.Guild
{
    [Table("Timers")]
    public class Timer
    {
        [Key, Column("GuildId", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long GuildId { get; set; }

        [Key, Column("UserId", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long UserId { get; set; }

        public DateTime Value { get; set; }
    }
}
