using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Models
{
    public class IsDonator
    {
		public long UserId { get; set; }

		public int TotalPaidCents { get; set; }
		public DateTime ValidUntil { get; set; }
		public int KeysRedeemed { get; set; }
    }
}
