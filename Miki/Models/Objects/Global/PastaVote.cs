using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Miki.Models
{
    [Table("Votes")]
    public class PastaVote
    {
        [Key, Column("Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; }

        [Key, Column("UserId", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long UserId { get; set; }

        [Column("PositiveVote")]
        public bool PositiveVote { get; set; }

        public GlobalPasta GetParent(MikiContext context)
        {
            return context.Pastas.Find(Id);
        }
    }
}