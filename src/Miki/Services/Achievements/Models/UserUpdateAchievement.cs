using System;
using System.Threading.Tasks;
using Miki.Discord;
using Miki.Framework;

namespace Miki.Accounts.Achievements.Objects
{
	internal class UserUpdateAchievement : IAchievement
	{
		public Func<UserUpdatePacket, ValueTask<bool>> CheckUserUpdate;

		public string Name { get; set; }

		public string ParentName { get; set; }

		public string Icon { get; set; }

		public int Points { get; set; }

        public Task InitAsync(MikiApp app)
        {
            app.GetService<DiscordClient>().UserUpdate += (user, newUser)
                => this.CheckAsync(new UserUpdatePacket
                {
                    discordChannel = null,
                    discordUser = newUser,
                    UserOld = user
                }).AsTask();
            return Task.CompletedTask;
        }

        public ValueTask<bool> CheckAsync(BasePacket packet)
        {
            return CheckUserUpdate(packet as UserUpdatePacket);
        }
    }
}