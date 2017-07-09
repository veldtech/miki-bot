using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models
{
    [Table("Connections")]
    class Connection
    {
        [Key]
        [Column("DiscordUserId")]
        public long DiscordUserId { get; set; }

        [Column("PatreonUserId")]
        public string PatreonUserId { get; set; }
    }
}
