using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models
{
	public class PinnedBadges
	{
		[Key, Column("id")]
		public long Id { get; set; }

		[Column("badges")]
		public string _Badges
		{
			get;
			set;
		}

		public void SetPinnedBadge(int index, int value)
		{
			string[] badges = _Badges.Split(',');
			badges[index] = value.ToString();
			_Badges = string.Join(",", badges);
		}
	}
}
