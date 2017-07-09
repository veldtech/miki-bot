using IA;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models
{
    [Table("GlobalPastas")]
    public class GlobalPasta
    {
        [Key]
        [Column("Id")]
        public string Id { get; set; }

        [Column("Content")]
        public string Text { get; set; }

        [Column("CreatorID")]
        public long creator_id { get; set; }

        [Column("DateCreated")]
        public DateTime date_created { get; set; }

        [Column("TimesUsed")]
        public int TimesUsed { get; set; }

        [NotMapped]
        public ulong CreatorId
        {
            get
            {
                unchecked
                {
                    return (ulong)creator_id;
                }
            }
            set
            {
                unchecked
                {
                    creator_id = (long)value;
                }
            }
        }

        public bool CanDeletePasta(ulong user_id)
        {
            return user_id == CreatorId || Bot.instance.Events.Developers.Contains(user_id);
        }

        public VoteCount GetVotes(MikiContext context)
        {
            return context.Database.SqlQuery<VoteCount>("select count(CASE WHEN PositiveVote = 1 THEN 1 END) as upvotes, count(CASE WHEN PositiveVote = 0 THEN 1 END) as downvotes from [Votes] where Id=@p0", Id).First();
        }
    }

    class PastaSearchResult
    {
        [Key]
        [Column("id")]
        public string Id { get; set; }

        [Column("total_count")]
        public int Total_Count { get; set; }
    }
}
