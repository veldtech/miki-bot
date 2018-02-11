using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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