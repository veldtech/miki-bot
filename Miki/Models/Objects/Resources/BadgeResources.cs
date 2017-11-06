using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models
{
	public class BadgeResources
	{
		[Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
		public long Id { get; set; }

		[Column("badges_owned")]
		public virtual ICollection<BadgesOwned> BadgesOwned { get; set; }

		[Column("creator_id")]
		public long? CreatorId { get; set; }

		[Column("creator")]
		public User Creator { get; set; }

		[Column("date_added")]
		public DateTime DateAdded { get; set; } = DateTime.Now;

		[Column("name")]
		public string Name { get; set; }

		[Column("url")]
		public string Url { get; set; }
	}
}
