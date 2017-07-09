using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models
{
    [Table("ChannelLanguage")]
    public class ChannelLanguage
    {
        [Key]
        [Column("EntityId")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long EntityId { get; set; }

        [Column("Language")]
        public string Language { get; set; } = "en-US";
    }
}
