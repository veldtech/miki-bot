using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.Configuration;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Common.Packets;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Logging;
using StatsdClient;
using System;
using System.Threading.Tasks;

namespace Miki.Modules.Internal.Services
{
    public class DatadogService : BaseService
    {
        public override void Install(Module m)
        {
            DogStatsd.Configure(new StatsdConfig
            {
                // TODO #534: Change to [Configurable]
                StatsdServerName = Global.Config.DatadogHost,
                StatsdPort = 8125,
                Prefix = "miki"
            });

            base.Install(m);

            CreateAccountMetrics();

            CreateAchievementsMetrics();

            CreateEventSystemMetrics(m.EventSystem);

            CreateDiscordMetrics();

            CreateHttpMetrics();

            Log.Message("Datadog set up!");
        }

        private void CreateAccountMetrics()
        {
            var accounts = AccountManager.Instance;
            if (accounts == null)
            {
                return;
            }

            accounts.OnGlobalLevelUp += (user, channel, level) =>
            {
                DogStatsd.Counter("levels.global", level, 1, new[]{
                    $"level:{level}"
                });
                return Task.CompletedTask;
            };
            accounts.OnLocalLevelUp += (user, channel, level) =>
            {
                DogStatsd.Counter("levels.local", level, 1, new[]{
                    $"level:{level}"
                });
                return Task.CompletedTask;
            };
        }
        private void CreateAchievementsMetrics()
        {
            var achievements = AchievementManager.Instance;
            if(achievements == null)
            {
                return;
            }

            achievements.OnAchievementUnlocked += (achievement) =>
            {
                DogStatsd.Increment("achievements.gained");
                return Task.CompletedTask;
            };
        }
        private void CreateDiscordMetrics()
        {
            var discord = MikiApp.Instance.Discord;
            if (discord == null)
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
        private void CreateEventSystemMetrics(EventSystem system)
        {
            if (system == null)
            {
                return;
            }

            var defaultHandler = system.GetCommandHandler<SimpleCommandHandler>();
            if (defaultHandler != null)
            {
                defaultHandler.OnMessageProcessed += OnMessageProcessed;
            }
        }
        private void CreateHttpMetrics()
        {
            var discordHttpClient = MikiApp.Instance
               .GetService<DiscordApiClient>()?.RestClient;
            if (discordHttpClient == null)
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

        private Task OnMessageProcessed(CommandEvent ev, IDiscordMessage msg, long timeMs)
        {
            if (ev.Module == null)
            {
                return Task.CompletedTask;
            }

            DogStatsd.Histogram("commands.time", timeMs, 0.1, new[] {
                $"commandtype:{ev.Module.Name.ToLowerInvariant()}",
                $"commandname:{ev.Name.ToLowerInvariant()}"
            });

            DogStatsd.Counter("commands.count", 1, 1, new[] {
                $"commandtype:{ev.Module.Name.ToLowerInvariant()}",
                $"commandname:{ev.Name.ToLowerInvariant()}"
            });

            return Task.CompletedTask;
        }
	}
}