using Miki.Framework;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;


namespace Miki.Models
{
    public class Marriage
    {
		public long MarriageId { get; set; }
		public List<UserMarriedTo> Participants { get; set; }
        public bool IsProposing { get; set; }
        public DateTime TimeOfMarriage { get; set; }
        public DateTime TimeOfProposal { get; set; }

		public ulong GetMe(ulong id) 
			=> GetMe(id.ToDbLong()).FromDbLong();
        public long GetMe(long id)
        {
			return Participants.FirstOrDefault(x => x.UserId == id)?.UserId ?? 0;
        }

        public ulong GetOther(ulong id) 
			=> GetOther(id.ToDbLong()).FromDbLong();
        public long GetOther(long id)
        {
			return Participants.FirstOrDefault(x => x.UserId != id)?.UserId ?? 0;
		}

		public void AcceptProposal(MikiContext context)
        {
            TimeOfMarriage = DateTime.Now;
            IsProposing = false;
        }

        public async Task RemoveAsync(MikiContext context)
        {
            context.Marriages.Remove(this);
            await context.SaveChangesAsync();
        }
		
        public static async Task DeclineAllProposalsAsync(MikiContext context, long me)
        {
            List<Marriage> proposals = await InternalGetProposalsReceivedAsync(context, me);
			proposals.AddRange(await InternalGetProposalsSentAsync(context, me));
            List<Marriage> disposableProposals = new List<Marriage>();
            context.Marriages.RemoveRange(proposals);
            await context.SaveChangesAsync();
        }

        public static async Task DivorceAllMarriagesAsync(MikiContext context, long me)
        {
            List<Marriage> Marriages = await InternalGetMarriagesAsync(context, me);
            context.Marriages.RemoveRange(Marriages);
            await context.SaveChangesAsync();
        }

        public static async Task<bool> ExistsAsync(MikiContext context, ulong id1, ulong id2) 
			=> await ExistsAsync(context, id1.ToDbLong(), id2.ToDbLong());
        public static async Task<bool> ExistsAsync(MikiContext context, long id1, long id2)
			=> await GetEntryAsync(context, id1, id2) != null;

		public static async Task<bool> ExistsAsMarriageAsync(MikiContext context, long id1, long id2)
			=> await GetMarriageAsync(context, id1, id2) != null;

		public static async Task<List<Marriage>> GetProposalsSent(MikiContext context, long asker) 
			=> await InternalGetProposalsSentAsync(context, asker);

        public static async Task<List<Marriage>> GetProposalsReceived(MikiContext context, long asker) 
			=> await InternalGetProposalsReceivedAsync(context, asker);

        public static async Task<Marriage> GetMarriageAsync(MikiContext context, ulong receiver, ulong asker) 
			=> await GetMarriageAsync(context, receiver.ToDbLong(), asker.ToDbLong());
        public static async Task<Marriage> GetMarriageAsync(MikiContext context, long receiver, long asker)
        {
            Marriage m = null;
            m = await InternalGetMarriageAsync(context, receiver, asker);
            if (m == null) m = await InternalGetMarriageAsync(context, asker, receiver);
            return m;
        }

        public static async Task<List<Marriage>> GetMarriagesAsync(MikiContext context, long userid)
        {
            return await InternalGetMarriagesAsync(context, userid);
        }

        public static async Task<Marriage> GetEntryAsync(MikiContext context, ulong receiver, ulong asker) 
			=> await GetEntryAsync(context, receiver.ToDbLong(), asker.ToDbLong());
        public static async Task<Marriage> GetEntryAsync(MikiContext context, long receiver, long asker)
        {
            Marriage m = null;
            m = await context.Marriages
				.Where(x => 
					x.Participants
						.Any(c => c.UserId == receiver) && 
					x.Participants
						.Any(c => c.UserId == asker))
				.FirstOrDefaultAsync();
            return m;
        }

        public static async Task<bool> IsBeingProposedBy(MikiContext context, long MarriageId)
        {
            return await InternalGetProposalAsync(context, MarriageId) != null;
        }

        public static async Task<bool> ProposeAsync(long receiver, long asker)
        {
			try
			{
				using (var context = new MikiContext())
				{
					Marriage m = context.Marriages.Add(new Marriage()
					{
						IsProposing = true,
						TimeOfProposal = DateTime.Now,
						TimeOfMarriage = DateTime.Now,
					}).Entity;

					await context.SaveChangesAsync();

					context.UsersMarriedTo.Add(new UserMarriedTo()
					{
						MarriageId = m.MarriageId,
						UserId = receiver
					});

					context.UsersMarriedTo.Add(new UserMarriedTo()
					{
						MarriageId = m.MarriageId,
						UserId = asker,
						Asker = true
					});

					await context.SaveChangesAsync();
					return true;
				}
			}
			catch(Exception e)
			{
				Log.Message(e.Message + "\n" + e.StackTrace);
			}
			return false;
        }

        private static async Task<Marriage> InternalGetProposalAsync(MikiContext context, long MarriageId)
        {
			return await context
				.Marriages
				.Include(x => x.Participants)
				.FirstAsync(x => x.MarriageId == MarriageId);
        }

		private static async Task<List<Marriage>> InternalGetProposalsSentAsync(MikiContext context, long asker)
		{
			return await context.Marriages
				.Where(p => p.Participants.FirstOrDefault(x => x.UserId == asker && x.Asker) != null && p.IsProposing == true)
					.Include(x => x.Participants)
				.ToListAsync();
		}

        private static async Task<List<Marriage>> InternalGetProposalsReceivedAsync(MikiContext context, long receiver)
        {
            return await context.Marriages
				.Where(p => p.Participants.FirstOrDefault(x => x.UserId == receiver && !x.Asker) != null && p.IsProposing == true)
					.Include(x => x.Participants)
				.ToListAsync();
        }

        private static async Task<Marriage> InternalGetMarriageAsync(MikiContext context, long receiver, long asker)
        {
            return await context.Marriages
				.Where(tm => tm.Participants.FirstOrDefault(x => x.UserId == receiver) != null && tm.Participants.FirstOrDefault(x => x.UserId == receiver) != null && tm.IsProposing == false)
					.Include(x => x.Participants)
				.FirstOrDefaultAsync();
        }

        private async static Task<List<Marriage>> InternalGetMarriagesAsync(MikiContext context, long userid)
        {
            return await context
                .Marriages
                .Where(p => p.Participants.FirstOrDefault(x => x.UserId == userid) != null && !p.IsProposing)
					.Include(x => x.Participants)
				.ToListAsync();
        }
    }
}