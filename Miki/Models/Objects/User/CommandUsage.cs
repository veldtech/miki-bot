using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Miki.Models
{
    public class CommandUsage
    {
        public long UserId { get; set; }
        public string Name { get; set; }
        public int Amount { get; set; }

		public User User { get; set; }
    }
}