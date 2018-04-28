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
		public UserMarriedTo Participants { get; set; }
        public bool IsProposing { get; set; }
        public DateTime TimeOfMarriage { get; set; }
        public DateTime TimeOfProposal { get; set; }

        public ulong GetOther(ulong id) 
			=> GetOther(id.ToDbLong()).FromDbLong();
        public long GetOther(long id)
        {
			return Participants.AskerId == id ? Participants.ReceiverId : Participants.AskerId;
		}

		public void AcceptProposal(MikiContext context)
        {
            TimeOfMarriage = DateTime.Now;
            IsProposing = false;
        }
		
        public static async Task DeclineAllProposalsAsync(MikiContext context, long me)
        {
            List<UserMarriedTo> proposals = await InternalGetProposalsReceivedAsync(context, me);

			proposals.AddRange(await InternalGetProposalsSentAsync(context, me));

			context.Marriages.RemoveRange(proposals.Select(x => x.Marriage).ToList());
			context.UsersMarriedTo.RemoveRange(proposals);

			await context.SaveChangesAsync();
        }

        public static async Task DivorceAllMarriagesAsync(MikiContext context, long me)
        {
            List<UserMarriedTo> Marriages = await InternalGetMarriagesAsync(context, me);
            context.Marriages.RemoveRange(Marriages.Select(x => x.Marriage));
            await context.SaveChangesAsync();
        }

        public static async Task<bool> ExistsAsync(MikiContext context, ulong id1, ulong id2) 
			=> await ExistsAsync(context, id1.ToDbLong(), id2.ToDbLong());
        public static async Task<bool> ExistsAsync(MikiContext context, long id1, long id2)
			=> await GetEntryAsync(context, id1, id2) != null;

		public static async Task<bool> ExistsAsMarriageAsync(MikiContext context, long id1, long id2)
			=> await GetMarriageAsync(context, id1, id2) != null;

		public static async Task<List<UserMarriedTo>> GetProposalsSent(MikiContext context, long asker) 
			=> await InternalGetProposalsSentAsync(context, asker);

        public static async Task<List<UserMarriedTo>> GetProposalsReceived(MikiContext context, long userid) 
			=> await InternalGetProposalsReceivedAsync(context, userid);

        public static async Task<UserMarriedTo> GetMarriageAsync(MikiContext context, ulong receiver, ulong asker) 
			=> await GetMarriageAsync(context, receiver.ToDbLong(), asker.ToDbLong());
        public static async Task<UserMarriedTo> GetMarriageAsync(MikiContext context, long receiver, long asker)
        {
			UserMarriedTo m = null;
            m = await InternalGetMarriageAsync(context, receiver, asker);
            if (m == null) m = await InternalGetMarriageAsync(context, asker, receiver);
            return m;
        }

        public static async Task<List<UserMarriedTo>> GetMarriagesAsync(MikiContext context, long userid)
        {
            return await InternalGetMarriagesAsync(context, userid);
        }

        public static async Task<UserMarriedTo> GetEntryAsync(MikiContext context, ulong receiver, ulong asker) 
			=> await GetEntryAsync(context, receiver.ToDbLong(), asker.ToDbLong());
        public static async Task<UserMarriedTo> GetEntryAsync(MikiContext context, long receiver, long asker)
        {
            UserMarriedTo m = null;
            m = await context.UsersMarriedTo
				.Include(x => x.Marriage).FirstOrDefaultAsync(x => (x.AskerId == asker && x.ReceiverId == receiver) || (x.AskerId == receiver && x.ReceiverId == asker));
            return m;
        }

		public static async Task ProposeAsync(long asker, long receiver)
		{
			using (var context = new MikiContext())
			{
				Marriage m = context.Marriages.Add(new Marriage()
				{
					IsProposing = true,
					TimeOfProposal = DateTime.Now,
					TimeOfMarriage = DateTime.Now,
				}).Entity;

				context.UsersMarriedTo.Add(new UserMarriedTo()
				{
					MarriageId = m.MarriageId,
					ReceiverId = receiver,
					AskerId = asker
				});

				await context.SaveChangesAsync();
			}
		}

		/// <summary>
		/// gets specific proposal
		/// </summary>
		/// <param name="context"></param>
		/// <param name="askerid"></param>
		/// <param name="userid"></param>
		/// <returns></returns>
        private static async Task<UserMarriedTo> InternalGetProposalAsync(MikiContext context, long askerid, long userid)
        {
			return await context.UsersMarriedTo.Include(x => x.Marriage)
				.FirstOrDefaultAsync(x => (x.AskerId == askerid || x.ReceiverId == userid) && x.Marriage.IsProposing);
		}

		/// <summary>
		/// gets the proposals sent to userid
		/// </summary>
		/// <param name="context"></param>
		/// <param name="asker"></param>
		/// <returns></returns>
		private static async Task<List<UserMarriedTo>> InternalGetProposalsSentAsync(MikiContext context, long asker)
		{
			var allInstances = await context.UsersMarriedTo.Include(x => x.Marriage)
				.Where(x => x.AskerId == asker && x.Marriage.IsProposing)
				.ToListAsync();
			return allInstances;
		}

		/// <summary>
		/// Gets the proposals received by userid
		/// </summary>
		/// <param name="context"></param>
		/// <param name="userid"></param>
		/// <returns></returns>
		private static async Task<List<UserMarriedTo>> InternalGetProposalsReceivedAsync(MikiContext context, long userid)
        {
			var allInstances = await context.UsersMarriedTo
				.Include(x => x.Marriage)
				.Where(x => x.ReceiverId == userid && x.Marriage.IsProposing)
				.ToListAsync();
			return allInstances;
		}

		/// <summary>
		/// Gets marriage instance of receiver and asker
		/// </summary>
		/// <param name="context"></param>
		/// <param name="receiver"></param>
		/// <param name="asker"></param>
		/// <returns></returns>
		private static async Task<UserMarriedTo> InternalGetMarriageAsync(MikiContext context, long receiver, long asker)
        {
			return await context.UsersMarriedTo
				.Include(x => x.Marriage)
				.FirstOrDefaultAsync(x => x.ReceiverId == receiver && x.AskerId == asker && !x.Marriage.IsProposing);
		}

		/// <summary>
		/// Gets all marriages the user id is married to
		/// </summary>
		/// <param name="context"></param>
		/// <param name="userid"></param>
		/// <returns></returns>
        private async static Task<List<UserMarriedTo>> InternalGetMarriagesAsync(MikiContext context, long userid)
        {
			var allInstances = await context.UsersMarriedTo
				.Include(x => x.Marriage)
				.Where(x => (x.ReceiverId == userid || x.AskerId == userid) && !x.Marriage.IsProposing)
				.ToListAsync();
			return allInstances;
        }
    }
}