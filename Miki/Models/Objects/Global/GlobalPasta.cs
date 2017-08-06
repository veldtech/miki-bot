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

        public bool CanDeletePasta(ulong user_id)
        {
            return user_id == creator_id.FromDbLong() || Bot.instance.Events.Developers.Contains(user_id);
        }

        public VoteCount GetVotes(MikiContext context)
        {
            VoteCount c = new VoteCount();
            c.Upvotes = context.Votes.Where(x => x.Id == Id && x.PositiveVote == true).Count();
            c.Downvotes = context.Votes.Where(x => x.Id == Id && x.PositiveVote == false).Count();
            return c;
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
