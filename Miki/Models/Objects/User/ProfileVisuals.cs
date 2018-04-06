using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Models.Objects.User
{
    class ProfileVisuals
    {
		public long UserId { get; set; }
		public int BackgroundId { get; set; } = 0;
		public string ForegroundColor { get; set; } = "#000000";
		public string BackgroundColor { get; set; } = "#000000";
	}
}
