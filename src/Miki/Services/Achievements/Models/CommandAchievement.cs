namespace Miki.Accounts.Achievements.Objects
{
    using Miki.Framework;
    using System;
    using System.Threading.Tasks;

    internal class CommandAchievement : IAchievement
	{
		public Func<CommandPacket, ValueTask<bool>> CheckCommand;

		public string Name { get; set; }
		public string ParentName { get; set; }
		public string Icon { get; set; }
		public int Points { get; set; }

        public Task InitAsync(MikiApp service)
        {
            // TODO(@velddev) Implement event for command completed in CommandPipeline
            //service.GetService<CommandPipeline>();//.OnCommandOrSomething += CheckAsync;

            return Task.CompletedTask;
        }

        public async ValueTask<bool> CheckAsync(BasePacket packet)
		{
			return await this.CheckCommand(packet as CommandPacket);
		}
	}
}