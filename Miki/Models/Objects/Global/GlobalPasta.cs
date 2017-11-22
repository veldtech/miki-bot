using IA;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
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

		[Column("score")]
		public int Score { get; set; }

		[Column("TimesUsed")]
		public int TimesUsed { get; set; }

		public async Task<int> GetScoreAsync()
		{
			using (var c = new MikiContext())
			{
				var votes = await GetVotesAsync(c);
				return votes.Upvotes - votes.Downvotes;
			}
		}


		public bool CanDeletePasta(ulong user_id)
        {
            return user_id == creator_id.FromDbLong() || Bot.instance.Events.Developers.Contains(user_id);
        }

        public async Task<VoteCount> GetVotesAsync(MikiContext context)
        {
            VoteCount c = new VoteCount();
            c.Upvotes = await context.Votes
				.Where(x => x.Id == Id && x.PositiveVote == true)
				.CountAsync();
            c.Downvotes = await context.Votes
				.Where(x => x.Id == Id && x.PositiveVote == false)
				.CountAsync();
            return c;
        }
    }

    internal class PastaSearchResult
    {
        [Key]
        [Column("id")]
        public string Id { get; set; }

        [Column("total_count")]
        public int Total_Count { get; set; }
    }
}