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

        [Column("TimesRemarried")]
        public int TimesRemarried { get; set; }

        [Column("Proposing")]
        public bool Proposing { get; set; }

        [Column("Divorced")]
        public bool Divorced { get; set; }

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
            if (Proposing)
            {
                TimeOfMarriage = DateTime.Now;
                Proposing = false;
                if (Divorced)
                {
                    Divorced = false;
                    TimesRemarried++;
                }
            }
        }

        public async Task DeclineProposalAsync(MikiContext context)
        {
            if(TimesRemarried > 0)
            {
                Proposing = false;
                await context.SaveChangesAsync();
                return;
            }
            context.Marriages.Remove(this);
            await context.SaveChangesAsync();
        }

        public async Task DivorceAsync(MikiContext context)
        {
            Divorced = true;
            await context.SaveChangesAsync();
        }

        #region Static Methods

        public static async Task DeclineAllProposalsAsync(MikiContext context, long me)
        {
            List<Marriage> proposals = InternalGetProposalsReceived(context, me);
            List<Marriage> disposableProposals = new List<Marriage>();
            foreach (Marriage m in proposals)
            {
                if(m.TimesRemarried == 0)
                {
                    disposableProposals.Add(m);
                    continue;
                }
                m.Proposing = false;
                m.Divorced = true;
            }
            context.Marriages.RemoveRange(disposableProposals);
            await context.SaveChangesAsync();
        }
        public static async Task DivorceAllMarriagesAsync(MikiContext context, long me)
        {
            List<Marriage> marriages = InternalGetMarriages(context, me);
            foreach(Marriage m in marriages)
            {
                m.Divorced = true;
            }
            await context.SaveChangesAsync();
        }

        public static async Task<bool> ExistsAsync(MikiContext context, ulong id1, ulong id2) => await ExistsAsync(context, id1.ToDbLong(), id2.ToDbLong());
        public static async Task<bool> ExistsAsync(MikiContext context, long id1, long id2)
        {
            return await GetEntryAsync(context, id1, id2) != null;
        }

        public static Marriage GetProposal(MikiContext context, ulong receiver, ulong asker) => GetProposal(context, receiver.ToDbLong(), asker.ToDbLong());
        public static Marriage GetProposal(MikiContext context, long receiver, long asker)
        {
            Marriage m = null;
            m = InternalGetProposal(context, receiver, asker);
            if (m == null) m = InternalGetProposal(context, asker, receiver);
            return m;
        }

        public static Marriage GetProposalReceived(MikiContext context, ulong receiver, ulong asker) => GetProposalReceived(context, receiver.ToDbLong(), asker.ToDbLong());
        public static Marriage GetProposalReceived(MikiContext context, long receiver, long asker)
        {
            Marriage m = null;
            m = InternalGetProposal(context, receiver, asker);
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

        public static bool IsBeingProposedBy(MikiContext context, long receiver, long asker)
        {
            return InternalGetProposal(context, receiver, asker) != null;
        }

        public static async Task<bool> ProposeAsync(MikiContext context, long receiver, long asker)
        {
            if(await ExistsAsync(context, receiver, asker))
            {
                return false;
            }

            context.Marriages.Add(new Marriage()
            {
                Id1 = receiver,
                Id2 = asker,
                Divorced = false,
                Proposing = true,
                TimeOfProposal = DateTime.Now,
                TimeOfMarriage = DateTime.Now,
                TimesRemarried = 0
            });

            await context.SaveChangesAsync();
            return true;
        }

        private static Marriage InternalGetProposal(MikiContext context, long receiver, long asker)
        {
            return context
                .Marriages
                .Find(receiver, asker);
        }
        private static List<Marriage> InternalGetProposalsSent(MikiContext context, long asker)
        {
            return context.Marriages.Where(p => p.Id1 == asker && p.Proposing == true && p.Divorced == false).ToList();
        }
        private static List<Marriage> InternalGetProposalsReceived(MikiContext context, long receiver)
        {
            return context.Marriages.Where(p => p.Id2 == receiver && p.Proposing == true && p.Divorced == false).ToList();
        }

        private static Marriage InternalGetMarriage(MikiContext context, long receiver, long asker)
        {
            return context.Marriages.Where(tm => tm.Id1 == receiver && tm.Id2 == asker && tm.Proposing == false && tm.Divorced == false).FirstOrDefault();
        }
        private static List<Marriage> InternalGetMarriages(MikiContext context, long userid)
        {
            return context
                .Marriages
                .Where(p => p.Id1 == userid && p.Proposing == false && p.Divorced == false 
                    || p.Id2 == userid && p.Proposing == false && p.Divorced == false)
                .ToList();
        }
        #endregion
    }
}
