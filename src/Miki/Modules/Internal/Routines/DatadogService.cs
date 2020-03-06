namespace Miki.Modules.Internal.Routines
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Accounts;
    using Miki.Bot.Models;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Nodes;
    using Miki.Logging;
    using Miki.Services.Achievements;
    using StatsdClient;

    public class DatadogRoutine
	{
        public DatadogRoutine(
            AccountService accounts,
            AchievementService achievements,
            IAsyncEventingExecutor<IDiscordMessage> commandPipeline,
            Config config,
            IDiscordClient discordClient,
            DiscordApiClient discordApiClient)
        {
            if(string.IsNullOrWhiteSpace(config.DatadogHost))
            {
                Log.Warning("Metrics are not being collected");
                return;
            }

            DogStatsd.Configure(new StatsdConfig
            {
                // TODO #534: Change to [Configurable]
                StatsdServerName = config.DatadogHost,
                StatsdPort = 8125,
                Prefix = "miki"
            });

            CreateAccountMetrics(accounts);
            //CreateAchievementsMetrics(achievements);
            CreateEventSystemMetrics(commandPipeline);
            CreateDiscordMetrics(discordClient);

            Log.Message("Datadog set up!");
        }

        private void CreateAccountMetrics(AccountService service)
		{
            if(service == null)
            {
                return;
            }

            service.OnGlobalLevelUp += (user, channel, level) =>
            {
                DogStatsd.Counter("levels.global", 1, 1, new[]{
                    $"level:{level}"
                });
                return Task.CompletedTask;
            };
            service.OnLocalLevelUp += (user, channel, level) =>
            {
                DogStatsd.Counter("levels.local", 1, 1, new[]{
                    $"level:{level}"
                });
                return Task.CompletedTask;
            };
        }
		private void CreateAchievementsMetrics(AchievementService service)
		{
            if(service == null)
            {
                return;
            }

            service.OnAchievementUnlocked += (ctx, achievement) =>
            {
                DogStatsd.Increment(
                    "achievements.gained", tags: new[]
                    {
                        $"achievement:{achievement.ResourceName}"
                    });
                return Task.CompletedTask;
            };
        }
		private void CreateDiscordMetrics(IDiscordClient discord)
		{
			if(discord == null)
			{
				return;
			}

			discord.MessageCreate += (msg) =>
			{
				DogStatsd.Increment("messages.received");
				return Task.CompletedTask;
			};
			
            discord.GuildJoin += (newGuild) =>
			{
				DogStatsd.Increment("guilds.joined");
				return Task.CompletedTask;
			};
			discord.GuildLeave += (oldGuild) =>
			{
				DogStatsd.Increment("guilds.left");
				return Task.CompletedTask;
			};
		}

        private void CreateEventSystemMetrics(IAsyncEventingExecutor<IDiscordMessage> system)
        {
            if(system == null)
            {
                return;
            }

            system.OnExecuted += OnCommandProcessed;
        }

        private ValueTask OnCommandProcessed(IExecutionResult<IDiscordMessage> arg)
        {
            if(!(arg.Context.Executable is Node ev))
            {
                return default;
            }

            if(!arg.Success)
            {
                DogStatsd.Counter("commands.error", 1, 1, new[]
                {
                    $"commandtype:{ev.Parent.ToString().ToLowerInvariant()}",
                    $"commandname:{ev.ToString().ToLowerInvariant()}"
                });
            }
            else
            {
                DogStatsd.Counter("commands.count", 1, 1, new[] 
                {
                    $"commandtype:{ev.Parent.ToString().ToLowerInvariant()}",
                    $"commandname:{ev.ToString().ToLowerInvariant()}"
                });
            }
            return default;
        }
    }
}