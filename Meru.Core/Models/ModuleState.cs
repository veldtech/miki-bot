using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IA.Models
{
    [Table("ModuleStates")]
    internal class ModuleState
    {
        [Key]
        [Column("ModuleName", Order = 0)]
        public string ModuleName { get; set; }

        [Key]
        [Column("ChannelId", Order = 1)]
        public long ChannelId { get; set; }

        [Column("State")]
        public bool State { get; set; }
    }
}