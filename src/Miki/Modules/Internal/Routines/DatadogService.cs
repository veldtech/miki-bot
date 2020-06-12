using System;
using System.Threading.Tasks;
using Miki.Accounts;
using Miki.Bot.Models;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Localization.Exceptions;
using Miki.Logging;
using Miki.Services.Achievements;
using Miki.Utility;
using StatsdClient;

namespace Miki.Modules.Internal.Routines
{
    public class DatadogRoutine
    {
        public DatadogRoutine(
            AccountService accounts,
            AchievementService achievements,
            IAsyncEventingExecutor<IDiscordMessage> commandPipeline,
            Config config,
            IDiscordClient discordClient)
        {
            if(string.IsNullOrWhiteSpace(config.DatadogHost))
            {
                Log.Warning("Metrics are not being collected");
                return;
            }

            DogStatsd.Configure(new StatsdConfig
            {
                StatsdServerName = config.DatadogHost,
                StatsdPort = 8125,
                Prefix = "miki"
            });

            CreateAchievementsMetrics(achievements);
            CreateAccountMetrics(accounts);
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

            service.OnAchievementUnlocked.Subscribe(res =>
            {
                var (_, achievement) = res;
                DogStatsd.Increment(
                    "achievements.gained", tags: new[]
                    {
                        $"achievement:{achievement.ResourceName}"
                    });
            });
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

            system.OnExecuted += OnCommandProcessedAsync;
        }

        private ValueTask OnCommandProcessedAsync(IExecutionResult<IDiscordMessage> arg)
        {
            if(!(arg.Context.Executable is Node ev))
            {
                return default;
            }
            
            var commandContext = new[]
            {
                $"commandtype:{ev.Parent.ToString().ToLowerInvariant()}",
                $"commandname:{ev.ToString().ToLowerInvariant()}",
                $"author:{arg.Context.GetAuthor().Id}",
                $"locale:{arg.Context.GetLocale().CountryCode}"
            };

            if(!arg.Success && !(arg.Error is LocalizedException))
            {
                DogStatsd.Counter("commands.error", 1, 1, commandContext);
            }
            else
            {
                DogStatsd.Counter("commands.count", 1, 1, commandContext);
            }

            DogStatsd.Histogram("commands.time", arg.TimeMilliseconds, 1, commandContext);
            return default;
        }
    }
}