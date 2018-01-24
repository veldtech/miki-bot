using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.API.Leaderboards
{
	public struct LeaderboardsOptions
	{
		public LeaderboardsType type;

		public ulong mentionedUserId;
		public ulong guildId;

		public int pageNumber;

		public string commandSpecified;

		public LeaderboardsOptions(LeaderboardsType type = LeaderboardsType.EXP, int pageNumber = 0, ulong mentionedUserId = 0, ulong guildId = 0, string commandSpecified = "")
		{
			this.type = type;
			this.pageNumber = pageNumber;
			this.mentionedUserId = mentionedUserId;
			this.guildId = guildId;
			this.commandSpecified = commandSpecified;
		}
	}
}
