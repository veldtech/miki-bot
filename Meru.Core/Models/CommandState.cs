using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IA.Models
{
    [Table("CommandStates")]
    internal class CommandState
    {
        [Key]
        [Column("CommandName", Order = 0)]
        public string CommandName { get; set; }

        [Key]
        [Column("ChannelId", Order = 1)]
        public long ChannelId { get; set; }

        [Column("State")]
        public bool State { get; set; }
    }
}