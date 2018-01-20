using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models
{
	public class BadgesOwned
	{
		public BadgesOwned(User user, long id)
		{
			Id = id;
			OwnerId = user.Id;
			Owner = user;
		}

		[Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
		public long Id { get; set; }	

		[Column("owner_id")]
		public long OwnerId { get; set; }
		
		[Column("owner")]
		public User Owner { get; set; }

		[Column("resources")]
		public virtual BadgeResources Resource { get; set; }
	}
}
