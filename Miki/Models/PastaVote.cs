using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public long __UserId { get; set; }

        [Column("PositiveVote")]
        public bool PositiveVote { get; set; }

        [NotMapped]
        public ulong UserId
        {
            get
            {
                unchecked
                {
                    return (ulong)__UserId;
                }
            }
            set
            {
                unchecked
                {
                    __UserId = (long)value;
                }
            }
        }

        public GlobalPasta GetParent(MikiContext context)
        {
            return context.Pastas.Find(Id);
        }
    }
}
