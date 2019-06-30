using Microsoft.EntityFrameworkCore;
using Miki.Bot.Models;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Commands.Localization;
using Miki.Framework.Commands.Permissions;
using Miki.Framework.Commands.Permissions.Attributes;
using Miki.Framework.Commands.Stages;
using Miki.Framework.Events;
using Miki.Localization;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

	[Module("settings")]
	internal class SettingsModule
	{
        private readonly IDictionary<DatabaseSettingId, Enum> _settingOptions = new Dictionary<DatabaseSettingId, Enum>()
        {
            {DatabaseSettingId.LevelUps, (LevelNotificationsSetting)0 },
            {DatabaseSettingId.Achievements, (AchievementNotificationSetting)0 }
        };

        [Command("listlocale")]
        public async Task ListLocaleAsync(IContext e)
        {
            var localeStage = e.GetService<LocalizationPipelineStage>();
            var locale = e.GetLocale();

            var localeNames = string.Join(", ",
                localeStage.LocaleNames.Keys
                    .Select(x => $"`{x}`"));

            await new EmbedBuilder()
                .SetTitle(locale.GetString("locales_available"))
                .SetDescription(localeNames)
                .AddField(
                    "Your language not here?",
                    locale.GetString(
                        "locales_contribute",
                        $"[{locale.GetString("locales_translations")}](https://poeditor.com/join/project/FIv7NBIReD)"))
                .ToEmbed()
                .QueueAsync(e.GetChannel());
        }

        [Command("setlocale")]
        public async Task SetLocale(IContext e)
        {
            var localization = e.GetStage<LocalizationPipelineStage>();

            string localeName = e.GetArgumentPack().Pack.TakeAll() ?? "";

            if (!localization.LocaleNames.TryGetValue(localeName, out string langId))
            {
                await e.ErrorEmbedResource(
                    "error_language_invalid",
                    localeName,
                    e.GetPrefixMatch()
                ).ToEmbed().QueueAsync(e.GetChannel());
            }

            await localization.SetLocaleForChannelAsync(e, (long)e.GetChannel().Id, langId);

            await e.SuccessEmbed(
                    e.GetLocale()
                    .GetString(
                        "localization_set",
                        $"`{localeName}`"))
                .QueueAsync(e.GetChannel());
        }

        [Command("setnotifications")]
        public async Task SetupNotifications(IContext e)
        {
            if (!e.GetArgumentPack().Take(out string enumString))
            {
                // TODO (Veld) : Handle error.
            }

            if (!enumString.TryFromEnum<DatabaseSettingId>(out var value))
            {
                await Utils.ErrorEmbedResource(e, new LanguageResource(
                    "error_notifications_setting_not_found",
                    string.Join(", ", Enum.GetNames(typeof(DatabaseSettingId))
                        .Select(x => $"`{x}`"))))
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            if (!_settingOptions.TryGetValue(value, out var @enum))
            {
                return;
            }

            if (!e.GetArgumentPack().Take(out string enumValue))
            {
            }

            if (!Enum.TryParse(@enum.GetType(), enumValue, true, out var type))
            {
                await Utils.ErrorEmbedResource(e, new LanguageResource(
                        "error_notifications_type_not_found",
                        enumValue,
                        value.ToString(),
                        string.Join(", ", Enum.GetNames(@enum.GetType())
                            .Select(x => $"`{x}`"))))
                    .ToEmbed()
                    .QueueAsync(e.GetChannel());
                return;
            }


            var context = e.GetService<MikiDbContext>();

            var channels = new List<IDiscordTextChannel> { e.GetChannel() };

            if (e.GetArgumentPack().CanTake)
            {
                if (e.GetArgumentPack().Take(out string attr))
                {
                    if (attr.StartsWith("-g"))
                    {
                        channels = (await e.GetGuild().GetChannelsAsync())
                            .Where(x => x.Type == ChannelType.GUILDTEXT)
                            .Select(x => x as IDiscordTextChannel)
                            .ToList();
                    }
                }
            }

            foreach (var c in channels)
            {
                await Setting.UpdateAsync(context, c.Id, value, (int)type);
            }
            await context.SaveChangesAsync();

            await Utils.SuccessEmbed(e, e.GetLocale().GetString("notifications_update_success"))
                .QueueAsync(e.GetChannel());
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
                    locale.GetString("miki_module_general_prefix_success_message", 
                    prefix))
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
                locale.GetString("setting_avatar_updated")	
			).QueueAsync(e.GetChannel());
		}
	}
}