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
    [Table("Marriages")]
    public class Marriage
    {
        [Key]
        [Column("Id1", Order = 0)]
        public long Id1 { get; set; }

        [Key]
        [Column("Id2", Order = 1)]
        public long Id2 { get; set; }

        [Column("Proposing")]
        public bool Proposing { get; set; }

        [Column("TimeOfMarriage")]
        public DateTime TimeOfMarriage { get; set; }

        [Column("TimeOfProposal")]
        public DateTime TimeOfProposal { get; set; }

        public ulong GetMe(ulong id) => GetMe(id.ToDbLong()).FromDbLong();
        public long GetMe(long id)
        {
            return (Id1 == id) ? Id1 : Id2;
        }

        public ulong GetOther(ulong id) => GetOther(id.ToDbLong()).FromDbLong();
        public long GetOther(long id)
        {
            return (Id1 == id) ? Id2 : Id1;
        }

        public void AcceptProposal(MikiContext context)
        {
            TimeOfMarriage = DateTime.Now;
            Proposing = false;
        }

        public async Task DeclineProposalAsync(MikiContext context)
        {
            context.Marriages.Remove(this);
            await context.SaveChangesAsync();
        }

        public async Task DivorceAsync(MikiContext context)
        {
            context.Marriages.Remove(this);
            await context.SaveChangesAsync();
        }

        #region Static Methods

        public static async Task DeclineAllProposalsAsync(MikiContext context, long me)
        {
            List<Marriage> proposals = InternalGetProposalsReceived(context, me);
            List<Marriage> disposableProposals = new List<Marriage>();
            context.Marriages.RemoveRange(proposals);
            await context.SaveChangesAsync();
        }
        public static async Task DivorceAllMarriagesAsync(MikiContext context, long me)
        {
            List<Marriage> marriages = InternalGetMarriages(context, me);
            context.Marriages.RemoveRange(marriages);
            await context.SaveChangesAsync();
        }

        public static async Task<bool> ExistsAsync(MikiContext context, ulong id1, ulong id2) => await ExistsAsync(context, id1.ToDbLong(), id2.ToDbLong());
        public static async Task<bool> ExistsAsync(MikiContext context, long id1, long id2)
        {
            return await GetEntryAsync(context, id1, id2) != null;
        }

        public static bool ExistsAsMarriage(MikiContext context, long id1, long id2)
        {
            return GetMarriage(context, id1, id2) != null;
        }

        public static async Task<Marriage> GetProposalAsync(MikiContext context, ulong receiver, ulong asker)
        {
            return await GetProposalAsync(context, receiver.ToDbLong(), asker.ToDbLong());
        }
        public static async Task<Marriage> GetProposalAsync(MikiContext context, long receiver, long asker)
        {
            Marriage m = null;
            m = await InternalGetProposalAsync(context, receiver, asker);
            if (m == null) m = await InternalGetProposalAsync(context, asker, receiver);
            return m;
        }

        public static async Task<Marriage> GetProposalReceivedAsync(MikiContext context, ulong receiver, ulong asker)
        {
            return await GetProposalReceivedAsync(context, receiver.ToDbLong(), asker.ToDbLong());
        }
        public static async Task<Marriage> GetProposalReceivedAsync(MikiContext context, long receiver, long asker)
        {
            Marriage m = null;
            m = await InternalGetProposalAsync(context, receiver, asker);
            return m;
        }

        public static List<Marriage> GetProposalsSent(MikiContext context, long asker) => InternalGetProposalsSent(context, asker);
        public static List<Marriage> GetProposalsReceived(MikiContext context, long asker) => InternalGetProposalsReceived(context, asker);

        public static Marriage GetMarriage(MikiContext context, ulong receiver, ulong asker) => GetMarriage(context, receiver.ToDbLong(), asker.ToDbLong());
        public static Marriage GetMarriage(MikiContext context, long receiver, long asker)
        {
            Marriage m = null;
            m = InternalGetMarriage(context, receiver, asker);
            if (m == null) m = InternalGetMarriage(context, asker, receiver);
            return m;
        }

        public static List<Marriage> GetMarriages(MikiContext context, long userid)
        {
            return InternalGetMarriages(context, userid);
        }

        public static async Task<Marriage> GetEntryAsync(MikiContext context, ulong receiver, ulong asker) => await GetEntryAsync(context, receiver.ToDbLong(), asker.ToDbLong());
        public static async Task<Marriage> GetEntryAsync(MikiContext context, long receiver, long asker)
        {
            Marriage m = null;
            m = await context.Marriages.FindAsync(receiver, asker);
            if(m == null)
            {
                m = await context.Marriages.FindAsync(asker, receiver);
            }
            return m;
        }

        public static async Task<bool> IsBeingProposedBy(MikiContext context, long receiver, long asker)
        {
            return await InternalGetProposalAsync(context, receiver, asker) != null;
        }

        public static async Task<bool> ProposeAsync(MikiContext context, long receiver, long asker)
        {
            context.Marriages.Add(new Marriage()
            {
                Id1 = receiver,
                Id2 = asker,
                Proposing = true,
                TimeOfProposal = DateTime.Now,
                TimeOfMarriage = DateTime.Now,
            });

            await context.SaveChangesAsync();
            return true;
        }

        private static async Task<Marriage> InternalGetProposalAsync(MikiContext context, long receiver, long asker)
        {
            return await context
                .Marriages
                .FindAsync(receiver, asker);
        }
        private static List<Marriage> InternalGetProposalsSent(MikiContext context, long asker)
        {
            return context.Marriages.Where(p => p.Id1 == asker && p.Proposing == true).ToList();
        }
        private static List<Marriage> InternalGetProposalsReceived(MikiContext context, long receiver)
        {
            return context.Marriages.Where(p => p.Id2 == receiver && p.Proposing == true).ToList();
        }

        private static Marriage InternalGetMarriage(MikiContext context, long receiver, long asker)
        {
            return context.Marriages.Where(tm => tm.Id1 == receiver && tm.Id2 == asker && tm.Proposing == false).FirstOrDefault();
        }
        private static List<Marriage> InternalGetMarriages(MikiContext context, long userid)
        {
            return context
                .Marriages
                .Where(p => p.Id1 == userid && p.Proposing == false
                    || p.Id2 == userid && p.Proposing == false)
                .ToList();
        }
        #endregion
    }
}
