namespace Miki.Modules
{
    using Miki.Framework.Commands.Localization.Models.Exceptions;
    using Miki.Framework.Commands.Prefixes;
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
    using Miki.Framework.Commands.Permissions.Attributes;
    using Miki.Framework.Commands.Permissions.Models;
    using Miki.Localization;
    using Miki.Services;
    using Amazon.S3;
    using Miki.Framework.Commands.Prefixes.Triggers;
    using Miki.Utility;

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

	[Module("settings")]
	internal class SettingsModule
    {
        private readonly IDictionary<DatabaseSettingId, Enum> settingOptions 
            = new Dictionary<DatabaseSettingId, Enum>
            {
                {DatabaseSettingId.LevelUps, (LevelNotificationsSetting) 0},
                {DatabaseSettingId.Achievements, (AchievementNotificationSetting) 0}
            };

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
                        $"[{locale.GetString("locales_translations")}](https://poeditor.com/join/project/FIv7NBIReD)"))
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
		public async Task SetupNotifications(IContext e)
        {
            var enumNames = string.Join(", ", Enum.GetNames(typeof(DatabaseSettingId))
                .Select(x => $"`{x}`"));
            if(!e.GetArgumentPack().Take(out DatabaseSettingId value))
			{
                await e.ErrorEmbedResource("error_notifications_setting_not_found", enumNames)
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
                return;
            }

            if(!settingOptions.TryGetValue(value, out var @enum))
			{
				return;
			}

            var enumValue = e.GetArgumentPack().TakeRequired<string>();
			
            var enumValueNames = string.Join(
                ", ", Enum.GetNames(@enum.GetType()).Select(x => $"`{x}`"));
            if(!Enum.TryParse(@enum.GetType(), enumValue, true, out var type))
			{
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
							.Where(x => x.Type == ChannelType.GuildText)
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
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
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
            await (trigger as DynamicPrefixTrigger)
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
                .QueueAsync(e, e.GetChannel());
        }

        [Command("syncavatar")]
		public async Task SyncAvatarAsync(IContext e)
		{
            var context = e.GetService<IUserService>();
            var cache = e.GetService<IExtendedCacheClient>();
            var amazonClient = e.GetService<AmazonS3Client>();

            var locale = e.GetLocale();
            await Utils.SyncAvatarAsync(
                e.GetAuthor(), cache, context, amazonClient);

			await e.SuccessEmbed(
                locale.GetString("setting_avatar_updated"))
                .QueueAsync(e, e.GetChannel());
		}
	}
}