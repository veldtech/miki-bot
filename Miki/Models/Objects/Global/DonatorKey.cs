using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Models
{
    public class DonatorKey
    {
		public Guid Key { get; set; }

		public TimeSpan StatusTime { get; set; }
    }
}
