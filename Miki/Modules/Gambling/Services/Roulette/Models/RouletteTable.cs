using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Miki.Modules.Gambling.Services.Roulette.Models
{
    [ProtoContract]
    public class RouletteTable
    {
        [ProtoMember(1)]
        public ulong CreatorId { get; set; }

        [ProtoMember(2)]
        public List<RouletteBet> Bets { get; set; }

        public RouletteTable()
        {

        }
        public RouletteTable(ulong creatorId)
        {
            CreatorId = creatorId;
        }

        public IEnumerable<RouletteBet> GetAllWinners(int roll)
        {
            return Bets.Where(x => x.HasWon(roll));
        }

        public void Reset()
        {
            if (Bets != null)
            {
                Bets.Clear();
            }
        }

        public int Roll()
        {
            return MikiRandom.Next(0, 36);
        }
    }
}
