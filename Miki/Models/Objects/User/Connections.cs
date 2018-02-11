using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Miki.Models
{
    public class Connection
    {
        public long DiscordUserId { get; set; }
        public string PatreonUserId { get; set; }

		public User User { get; set; }
	}
}