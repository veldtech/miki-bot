using IA;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Models
{
	public class GlobalPasta
	{
		public string Id { get; set; }
		public string Text { get; set; }
		public long CreatorId { get; set; }
		public DateTime CreatedAt { get; set; }
		public int Score { get; set; }
		public int TimesUsed { get; set; }

		public async Task<int> GetScoreAsync()
		{
			using (var c = new MikiContext())
			{
				var votes = await GetVotesAsync(c);
				return votes.Upvotes - votes.Downvotes;
			}
		}

		public bool CanDeletePasta(ulong userId)
        {
            return userId == CreatorId.FromDbLong() || 
				Bot.instance.Events.Developers.Contains(userId);
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

	public class PastaSearchResult
	{
		public string Id { get; set; }
		public int Count { get; set; }
	}
}