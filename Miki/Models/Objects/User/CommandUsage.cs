using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Miki.Models
{
    [Table("CommandUsages")]
    public class CommandUsage
    {
        [Key]
        [Column("UserId", Order = 0)]
        public long UserId { get; set; }

        [Key]
        [Column("Name", Order = 1)]
        public string Name { get; set; }

        [Column("Amount")]
        public int Amount { get; set; }
    }
}