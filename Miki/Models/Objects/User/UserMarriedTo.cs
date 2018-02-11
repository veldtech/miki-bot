namespace Miki.Models
{
    public class UserMarriedTo
    {
		public long UserId { get; set; }
		public long MarriageId { get; set; }
		public bool Asker { get; set; } = false;

		public User User { get; set; }
		public Marriage Marriage { get; set; }
    }
}
