namespace Miki.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;
    using Miki.Cache;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Attributes;
    using Miki.Framework.Commands.Localization;
    using Miki.Framework.Commands.Permissions.Attributes;
    using Miki.Framework.Commands.Permissions.Models;
    using Miki.Framework.Commands.Stages;
    using Miki.Localization;

    public enum LevelNotificationsSetting
	{
		RewardsOnly = 0,
		All = 1,
		NONE = 2
	}

    public enum AchievementNotificationSetting
    {
        All = 0,
        None = 1
    }

	[Module("settings")]
	internal class SettingsModule
	{
		private readonly IDictionary<DatabaseSettingId, Enum> _settingOptions = new Dictionary<DatabaseSettingId, Enum>()
		{
			{DatabaseSettingId.LevelUps, (LevelNotificationsSetting)0 },
			{DatabaseSettingId.Achievements, (AchievementNotificationSetting)0 }
		};

        private readonly Dictionary<string, string> languageNames = new Dictionary<string, string>()
        {
            { "arabic", "ara" },
            { "bulgarian", "bul" },
            { "czech", "cze" },
            { "danish", "dan" },
            { "dutch", "dut" },
            { "english", "eng" },
            { "finnish", "fin" },
            { "french", "fra" },
            { "german", "ger" },
            { "hebrew", "heb" },
            { "hindu", "hin" },
            { "hungarian", "hun" },
            { "italian", "ita" },
            { "japanese", "jpn" },
            { "lithuanian", "lit" },
            { "malaysian", "may" },
            { "norwegian", "nor" },
            { "polish", "pol" },
            { "portuguese", "por" },
            { "russian", "rus" },
            { "spanish", "spa" },
            { "swedish", "swe" },
            { "tagalog", "tgl" },
            { "ukrainian", "ukr" },
            { "chinese_simplified", "zhs" },
            { "chinese_traditional", "zht" }
        };

		[Command("listlocale")]
		public async Task ListLocaleAsync(IContext e)
		{
			var localeStage = e.GetService<LocalizationPipelineStage>();
			var locale = e.GetLocale();

			var localeNames = string.Join(",", languageNames.Keys.Select(x => $"`{x}`"));

            await new EmbedBuilder()
                .SetTitle(locale.GetString("locales_available"))
                .SetDescription(localeNames)
                .AddField(
                    "Your language not here?",
                    locale.GetString(
                        "locales_contribute",
                        $"[{locale.GetString("locales_translations")}](https://poeditor.com/join/project/FIv7NBIReD)"))
                .ToEmbed()
                .QueueAsync(e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("setlocale")]
        [DefaultPermission(PermissionStatus.Deny)]
		public async Task SetLocale(IContext e)
		{
			var service = e.GetService<ILocalizationService>();

			string localeIso = e.GetArgumentPack().Pack.TakeAll() ?? "";
			if(languageNames.TryGetValue(localeIso, out string langId))
            {
                localeIso = langId;
            }

            await service.SetLocaleAsync((long)e.GetChannel().Id, localeIso)
                .ConfigureAwait(false);

            var newLocale = await service.GetLocaleAsync((long)e.GetChannel().Id);

            await e.SuccessEmbed(
                    newLocale.GetString(
                        "localization_set", 
                        $"`{languageNames.FirstOrDefault(x => x.Value == localeIso).Key}`"))
                .QueueAsync(e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("setnotifications")]
		public async Task SetupNotifications(IContext e)
		{
			if(!e.GetArgumentPack().Take(out string enumString))
			{
				// TODO(velddev): Handle error.
			}

			if(!enumString.TryFromEnum<DatabaseSettingId>(out var value))
            {
                await e.ErrorEmbedResource(
                        "error_notifications_setting_not_found",
                        string.Join(", ", Enum.GetNames(typeof(DatabaseSettingId))
                            .Select(x => $"`{x}`")))
                    .ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

			if(!_settingOptions.TryGetValue(value, out var @enum))
			{
				return;
			}

			if(!e.GetArgumentPack().Take(out string enumValue))
			{
			}

			if(!Enum.TryParse(@enum.GetType(), enumValue, true, out var type))
			{
				await e.ErrorEmbedResource(
                        "error_notifications_type_not_found",
						enumValue,
						value.ToString(),
						string.Join(", ", Enum.GetNames(@enum.GetType())
							.Select(x => $"`{x}`")))
					.ToEmbed()
					.QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}


			var context = e.GetService<MikiDbContext>();

			var channels = new List<IDiscordTextChannel> { e.GetChannel() };

			if(e.GetArgumentPack().CanTake)
			{
				if(e.GetArgumentPack().Take(out string attr))
				{
					if(attr.StartsWith("-g"))
					{
						channels = (await e.GetGuild().GetChannelsAsync()
                                .ConfigureAwait(false))
							.Where(x => x.Type == ChannelType.GUILDTEXT)
							.Select(x => x as IDiscordTextChannel)
							.ToList();
					}
				}
			}

			foreach(var c in channels)
            {
                await Setting.UpdateAsync(context, c.Id, value, (int)type)
                    .ConfigureAwait(false);
            }

            await context.SaveChangesAsync()
                .ConfigureAwait(false);

            await e.SuccessEmbedResource("notifications_update_success")
                .QueueAsync(e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("showmodule")]
		public async Task ConfigAsync(IContext e)
		{
            var cache = e.GetService<ICacheClient>();
            var db = e.GetService<DbContext>();

            var commandHandler = e.GetStage<CommandHandlerStage>();

            string args = e.GetArgumentPack().Pack.TakeAll();

            var module = commandHandler.Modules
                .Where(x => x is NodeContainer)
                .Select(x => x as NodeContainer)
                .FirstOrDefault(x => x.Metadata.Identifiers
                    .Any(y => y.ToLowerInvariant() == args.ToLowerInvariant()));

            if (module != null)
            {
                // No module found
                return;
            }

            EmbedBuilder embed = new EmbedBuilder()
                .SetTitle(args.ToUpperInvariant());

            string content = "";

            foreach (Node ev in module.Children.OrderBy((x) => x.Metadata.Identifiers.First()))
            {
                var state = true;
                content += (state 
                    ? "<:iconenabled:341251534522286080>" 
                    : "<:icondisabled:341251533754728458>") + $" {ev.Metadata.Identifiers.First()}\n";
            }

            embed.AddInlineField("Events", content);

            content = "";

            await embed.ToEmbed()
                .QueueAsync(e.GetChannel());
        }

		[Command("setprefix")]
		public async Task PrefixAsync(IContext e)
		{
            var prefixMiddleware = e.GetStage<PipelineStageTrigger>();

            if (!e.GetArgumentPack().Take(out string prefix))
            {
                return;
            }

            await prefixMiddleware.GetDefaultTrigger()
                .ChangeForGuildAsync(
                    e.GetService<DbContext>(),
                    e.GetService<ICacheClient>(),
                    e.GetGuild().Id,
                    prefix);

            var locale = e.GetLocale();

            await new EmbedBuilder()
                .SetTitle(
                    locale.GetString("miki_module_general_prefix_success_header"))
                .SetDescription(
                    locale.GetString("miki_module_general_prefix_success_message", prefix))
                .AddField("Warning", "This command has been replaced with `>prefix set`.")
                .ToEmbed()
                .QueueAsync(e.GetChannel());
        }

		[Command("syncavatar")]
		public async Task SyncAvatarAsync(IContext e)
		{
            var context = e.GetService<MikiDbContext>();
            var cache = e.GetService<IExtendedCacheClient>();
            var locale = e.GetLocale();
            await Utils.SyncAvatarAsync(e.GetAuthor(), cache, context);

			await e.SuccessEmbed(
                locale.GetString("setting_avatar_updated"))
                .QueueAsync(e.GetChannel());
		}
	}
}