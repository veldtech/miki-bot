using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK
{
	public class DiscordGuild : IDiscordGuild
	{
		public string AvatarUrl => throw new NotImplementedException();

		public string Name => throw new NotImplementedException();

		public List<IDiscordRole> Roles => throw new NotImplementedException();

		public ulong Id => throw new NotImplementedException();

		public Task<int> GetChannelCountAsync()
		{
			throw new NotImplementedException();
		}

		public Task<List<IDiscordMessageChannel>> GetChannels()
		{
			throw new NotImplementedException();
		}

		public Task<IDiscordUser> GetCurrentUserAsync()
		{
			throw new NotImplementedException();
		}

		public Task<IDiscordMessageChannel> GetDefaultChannel()
		{
			throw new NotImplementedException();
		}

		public Task<IDiscordUser> GetOwnerAsync()
		{
			throw new NotImplementedException();
		}

		public IDiscordRole GetRole(ulong role_id)
		{
			throw new NotImplementedException();
		}

		public Task<IDiscordUser> GetUserAsync(ulong user_id)
		{
			throw new NotImplementedException();
		}

		public Task<int> GetUserCountAsync()
		{
			throw new NotImplementedException();
		}

		public Task<int> GetVoiceChannelCountAsync()
		{
			throw new NotImplementedException();
		}
	}
}