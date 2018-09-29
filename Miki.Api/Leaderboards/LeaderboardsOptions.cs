using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.API.Leaderboards
{
	// TODO: restructure
	public struct LeaderboardsOptions
	{
		public int Amount;

		public ulong? GuildId;

		public LeaderboardsType Type;

		public int Offset;
	}
}
