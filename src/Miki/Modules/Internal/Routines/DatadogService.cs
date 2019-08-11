namespace Miki.Modules.Internal.Routines
{
    using System;
    using Miki.Accounts;
    using Miki.Discord;
    using Miki.Discord.Rest;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using StatsdClient;
    using System.Threading.Tasks;
    using Miki.Framework.Commands.Nodes;
    using Miki.Logging;
    using Miki.Services.Achievements;

    public class DatadogRoutine
	{
        public void Install(NodeModule _, MikiApp app)
        {
            if(app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            DogStatsd.Configure(new StatsdConfig
            {
                // TODO #534: Change to [Configurable]
                StatsdServerName = Global.Config.DatadogHost,
                StatsdPort = 8125,
                Prefix = "miki"
            });
            
            CreateAccountMetrics(app.GetService<AccountService>());

            CreateAchievementsMetrics(app.GetService<AchievementService>());

            CreateEventSystemMetrics(app.GetService<CommandPipeline>());

            CreateDiscordMetrics();

            CreateHttpMetrics();

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

            service.OnAchievementUnlocked += (achievement) =>
            {
                DogStatsd.Increment(
                    "achievements.gained", tags: new[]
                    {
                        $"achievement:{achievement.achievement.Name}"
                    });
                return Task.CompletedTask;
            };
        }
		private void CreateDiscordMetrics()
		{
			var discord = MikiApp.Instance
				.GetService<DiscordClient>();
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

        private void CreateEventSystemMetrics(CommandPipeline system)
        {
            if(system == null)
            {
                return;
            }

            system.CommandProcessed += OnCommandProcessed;
        }

        private void CreateHttpMetrics()
		{
			var discordHttpClient = MikiApp.Instance
			   .GetService<DiscordApiClient>()?.RestClient;
			if(discordHttpClient == null)
			{
				return;
			}

			discordHttpClient.OnRequestComplete += (method, url) =>
			{
				DogStatsd.Histogram("discord.http.requests", 1, 1, new[] {
					$"http_method:{method}",
					$"http_uri:{url}"
				});
			};
		}

        private Task OnCommandProcessed(IContext arg)
        {
            if(!(arg.Executable is Node ev))
            {
                return Task.CompletedTask;
            }

            DogStatsd.Counter("commands.count", 1, 1, new[] {
                $"commandtype:{ev.Parent.ToString().ToLowerInvariant()}",
                $"commandname:{ev.ToString().ToLowerInvariant()}"
            });

            return Task.CompletedTask;
        }
    }
}