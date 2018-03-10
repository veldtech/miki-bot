using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Models
{
    public class DonatorKey
    {
		public Guid Key { get; set; }

		public TimeSpan StatusTime { get; set; }

		public static DonatorKey GenerateNew(TimeSpan? time = null)
		{
			using (var context = new MikiContext())
			{
				var key = context.DonatorKey.Add(new DonatorKey()
				{
					StatusTime = time ?? new TimeSpan(31, 0, 0, 0),
				}).Entity;
				context.SaveChanges();
				return key;
			}
		}
	}
}
