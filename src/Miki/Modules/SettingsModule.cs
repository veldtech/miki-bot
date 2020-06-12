using Miki.Framework.Commands.Localization.Models.Exceptions;
using Miki.Framework.Commands.Prefixes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Permissions.Attributes;
using Miki.Framework.Commands.Permissions.Models;
using Miki.Localization;
using Miki.Framework.Commands.Prefixes.Triggers;
using Miki.Services.Settings;
using Miki.Utility;
using Miki.Attributes;

namespace Miki.Modules
{
    public enum LevelNotificationsSetting
	{
		RewardsOnly = 0,
		All = 1,
		None = 2
	}

    public enum AchievementNotificationSetting
    {
        All = 0,
        None = 1
    }

	[Module("settings"), Emoji(AppProps.Emoji.Gear)]
	internal class SettingsModule
    {
        private readonly Dictionary<string, string> languageNames = new Dictionary<string, string>
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
			var locale = e.GetLocale();
            var localeNames = string.Join(", ", languageNames.Keys.Select(x => $"`{x}`"));

            await new EmbedBuilder()
                .SetTitle(locale.GetString("locales_available"))
                .SetDescription(localeNames)
                .AddField(
                    "Your language not here?",
                    locale.GetString(
                        "locales_contribute",
                        $"[{locale.GetString("locales_translations")}]({AppProps.Links.LocalizationInvite})"))
                .ToEmbed()
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("setlocale")]
        [DefaultPermission(PermissionStatus.Deny)]
		public async Task SetLocaleAsync(IContext e)
		{
            var service = e.GetService<ILocalizationService>();

            string localeIso = e.GetArgumentPack().Pack.TakeAll() ?? "";
			if(languageNames.TryGetValue(localeIso, out string langId))
            {
                localeIso = langId;
            }

            try
            {
                await service.SetLocaleAsync((long) e.GetChannel().Id, localeIso)
                    .ConfigureAwait(false);
            }
            catch (LocaleNotFoundException)
            {   
                await e.ErrorEmbedResource("error_locale_not_found", localeIso)
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }
            var localeName = languageNames.FirstOrDefault(x => x.Value == localeIso).Key;

            var newLocale = await service.GetLocaleAsync((long)e.GetChannel().Id);
            await newLocale.SuccessEmbedResource("localization_set", localeName)
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

        [Command("setnotifications")]
        public class SetNotificationsCommand
        {
            public readonly Dictionary<SettingType, Type> settingOptions =
                new Dictionary<SettingType, Type>
                {
                    {SettingType.Achievements, typeof(AchievementNotificationSetting)},
                    {SettingType.LevelUps, typeof(LevelNotificationsSetting)}
                };

            [Command]
            [DefaultPermission(PermissionStatus.Deny)]
            public async Task SetNotificationsAsync(IContext e)
            {
                if(!e.GetArgumentPack().Take(out SettingType value))
                {
                    var enumNames = string.Join(", ", Enum.GetNames(typeof(SettingType))
                        .Select(x => $"`{x}`"));
                    await e.ErrorEmbedResource("error_notifications_setting_not_found", enumNames)
                        .ToEmbed()
                        .QueueAsync(e, e.GetChannel())
                        .ConfigureAwait(false);
                    return;
                }

                if(!settingOptions.TryGetValue(value, out var enumType))
                {
                    throw new NotSupportedException();
                }

                var enumValue = e.GetArgumentPack().TakeRequired<string>();

                if(!Enum.TryParse(enumType, enumValue, true, out var type))
                {
                    var enumValueNames = string.Join(
                        ", ", Enum.GetNames(enumType).Select(x => $"`{x}`"));
                    await e.ErrorEmbedResource(
                            "error_notifications_type_not_found",
                            enumValue,
                            value.ToString(),
                            enumValueNames)
                        .ToEmbed()
                        .QueueAsync(e, e.GetChannel())
                        .ConfigureAwait(false);
                    return;
                }

                var settingService = e.GetService<ISettingsService>();
                var channels = new List<IDiscordTextChannel> { e.GetChannel() };

                if(e.GetArgumentPack().CanTake)
                {
                    if(e.GetArgumentPack().Take(out string attr))
                    {
                        if(attr.StartsWith("-g"))
                        {
                            channels = (await e.GetGuild().GetChannelsAsync()
                                    .ConfigureAwait(false))
                                .Where(x => x.Type == ChannelType.GuildText)
                                .Select(x => x as IDiscordTextChannel)
                                .ToList();
                        }
                    }
                }

                foreach(var c in channels)
                {
                    await settingService.SetAsync(value, (long)c.Id, (Enum)type)
                        .ConfigureAwait(false);
                }

                await e.SuccessEmbedResource("notifications_update_success")
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
            }
        }

        [Command("setprefix")]
        [DefaultPermission(PermissionStatus.Deny)]
		public async Task PrefixAsync(IContext e)
        {
            var prefix = e.GetArgumentPack().TakeRequired<string>();

            var prefixMiddleware = e.GetService<IPrefixService>();
            if(prefixMiddleware == null)
            {
                throw new InvalidOperationException("Cannot get service PrefixService");
            }

            var trigger = prefixMiddleware.GetDefaultTrigger();
            if(trigger is DynamicPrefixTrigger dynamicTrigger)
            {
                await dynamicTrigger.ChangeForGuildAsync(
                    e.GetService<IUnitOfWork>(),
                    e.GetService<ICacheClient>(),
                    e.GetGuild().Id,
                    prefix);
            }

            var locale = e.GetLocale();

            await new EmbedBuilder()
                .SetTitle(
                    locale.GetString("miki_module_general_prefix_success_header"))
                .SetDescription(
                    locale.GetString("miki_module_general_prefix_success_message", prefix))
                .AddField("Warning", "This command has been replaced with `>prefix set`.")
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }
    }
}