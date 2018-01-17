using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Miki.Models
{
    [Table("Connections")]
    internal class Connection
    {
        [Key]
        [Column("DiscordUserId")]
        public long DiscordUserId { get; set; }

        [Column("PatreonUserId")]
        public string PatreonUserId { get; set; }
    }
}