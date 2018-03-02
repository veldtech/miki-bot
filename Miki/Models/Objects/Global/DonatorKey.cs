using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Models
{
    public class DonatorKey
    {
		public Guid Key { get; set; }

		public TimeSpan StatusTime { get; set; }

		public static string GenerateNew(TimeSpan? time = null)
		{
			using (var context = new MikiContext())
			{
				string key = context.DonatorKey.Add(new DonatorKey()
				{
					StatusTime = time ?? new TimeSpan(31, 0, 0, 0),
				}).Entity.Key.ToString();
				context.SaveChanges();
				return key;
			}
		}
	}
}
