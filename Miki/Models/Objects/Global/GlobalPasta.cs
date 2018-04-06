using Miki.Framework;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Miki.Framework.Events;
using System.Text.RegularExpressions;

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

		public User User { get; set; }

		public async Task AddAsync(string id, string text, long creator)
		{
			if (Regex.IsMatch(text, "(http[s]://)?((discord.gg)|(discordapp.com/invite))/([A-Za-z0-9]+)", RegexOptions.IgnoreCase))
			{
				throw new Exception("You can't add ");
			}

			using (var context = new MikiContext())
			{
				GlobalPasta pasta = await context.Pastas.FindAsync(id);

				if (pasta != null)
				{
					//e.ErrorEmbed(e.GetResource("miki_module_pasta_create_error_already_exist")).Build().QueueToChannel(e.Channel);
					return;
				}

				context.Pastas.Add(new GlobalPasta()
				{
					Id = id,
					Text = text,
					CreatorId = creator,
					CreatedAt = DateTime.Now
				});
				await context.SaveChangesAsync();
			}
		}

		public bool CanDeletePasta(ulong userId)
		{
			return userId == CreatorId.FromDbLong() ||
				EventSystem.Instance.DeveloperIds.Contains(userId);
		}

		public async Task<int> GetScoreAsync()
		{
			using (var c = new MikiContext())
			{
				var votes = await GetVotesAsync(c);
				return votes.Upvotes - votes.Downvotes;
			}
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