using Microsoft.EntityFrameworkCore;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Dsl;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Framework.Languages;
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

	[Module(Name = "settings")]
	internal class SettingsModule
	{
        private readonly IDictionary<DatabaseSettingId, Enum> _settingOptions = new Dictionary<DatabaseSettingId, Enum>()
        {
            {DatabaseSettingId.LevelUps, (LevelNotificationsSetting)0 },
            {DatabaseSettingId.Achievements, (AchievementNotificationSetting)0 }
        };

        [Command(Name = "setnotifications", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SetupNotifications(EventContext e)
		{
            if (!e.Arguments.Take(out string enumString))
            {
                // TODO (Veld) : Handle error.
            }

            if (!enumString.TryFromEnum<DatabaseSettingId>(out var value))
            {
                await Utils.ErrorEmbedResource(e, new LanguageResource(
                    "error_notifications_setting_not_found",
                    string.Join(", ", Enum.GetNames(typeof(DatabaseSettingId))
                        .Select(x => $"`{x}`"))))
                    .ToEmbed().QueueToChannelAsync(e.Channel);
                return;
            }

            if (!_settingOptions.TryGetValue(value, out var @enum))
            {
                return;
            }

            if (!e.Arguments.Take(out string enumValue))
            {
            }

            if(!Enum.TryParse(@enum.GetType(), enumValue, true, out var type))
            {
                await Utils.ErrorEmbedResource(e, new LanguageResource(
                    "error_notifications_type_not_found",
                    enumValue,
                    value.ToString(),
                    string.Join(", ", Enum.GetNames(@enum.GetType())
                        .Select(x => $"`{x}`"))))
                    .ToEmbed().QueueToChannelAsync(e.Channel);
                return;
            }


            using (MikiContext context = new MikiContext())
			{
                IEnumerable<IDiscordTextChannel> channels 
                    = new List<IDiscordTextChannel> { e.Channel };

                if (e.Arguments.CanTake)
                {
                    
                    if (e.Arguments.Take(out string attr))
                    {
                        if (attr.StartsWith("-g"))
                        {
                            channels = (await e.Guild.GetChannelsAsync())
                                .Where(x => x.Type == ChannelType.GUILDTEXT)
                                .Select(x => x as IDiscordTextChannel);
                        }
                    }
                }

                foreach (var c in channels)
                {
                    await Setting.UpdateAsync(context, c.Id, value, (int)type);
                }
                await context.SaveChangesAsync();
			}

            await Utils.SuccessEmbed(e, e.Locale.GetString("notifications_update_success"))
                .QueueToChannelAsync(e.Channel);
        }

        [Command(Name = "showmodule")]
		public async Task ConfigAsync(EventContext e)
		{
            var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));
            var db = (DbContext)e.Services.GetService(typeof(DbContext));

            string args = e.Arguments.Pack.TakeAll();
            Module module = e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Modules.FirstOrDefault(x => x.Name.ToLower() == args.ToLower());

			if (module != null)
			{
				EmbedBuilder embed = new EmbedBuilder();

				embed.Title = (args.ToUpper());

				string content = "";

				foreach (CommandEvent ev in module.Events.OrderBy((x) => x.Name))
				{
					content += (await ev.IsEnabled(cache, db, e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
				}

				embed.AddInlineField("Events", content);

				content = "";

				foreach (BaseService ev in module.Services.OrderBy((x) => x.Name))
				{
					content += (await ev.IsEnabled(cache, db, e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
				}

				if (!string.IsNullOrEmpty(content))
					embed.AddInlineField("Services", content);

                await embed.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "showmodules")]
		public async Task ShowModulesAsync(EventContext e)
		{
            var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));
            var db = (DbContext)e.Services.GetService(typeof(DbContext));

            List<string> modules = new List<string>();
			SimpleCommandHandler commandHandler = e.EventSystem.GetCommandHandler<SimpleCommandHandler>();
			EventAccessibility userEventAccessibility = await commandHandler.GetUserAccessibility(e);

			foreach (CommandEvent ev in commandHandler.Commands)
			{
				if (userEventAccessibility >= ev.Accessibility)
				{
					if (ev.Module != null && !modules.Contains(ev.Module.Name.ToUpper()))
					{
						modules.Add(ev.Module.Name.ToUpper());
					}
				}
			}

			modules.Sort();

			string firstColumn = "", secondColumn = "";

			for (int i = 0; i < modules.Count(); i++)
			{
				string output = $"{(await e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Modules[i].IsEnabled(cache, db, e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>")} {modules[i]}\n";
				if (i < modules.Count() / 2 + 1)
				{
					firstColumn += output;
				}
				else
				{
					secondColumn += output;
				}
			}

            await new EmbedBuilder()
				.SetTitle($"Module Status for '{e.Channel.Name}'")
				.AddInlineField("Column 1", firstColumn)
				.AddInlineField("Column 2", secondColumn)
				.ToEmbed().QueueToChannelAsync(e.Channel);
		}

        [Command(Name = "setlocale", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SetLocale(EventContext e)
        {
            var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));

            using (var context = new MikiContext())
            {
                string localeName = e.Arguments.Pack.TakeAll() ?? "";

                if (Locale.LocaleNames.TryGetValue(localeName, out string langId))
                {
                    await Locale.SetLanguageAsync(context, e.Channel.Id, langId);

                    await e.SuccessEmbed(e.Locale.GetString("localization_set", $"`{localeName}`"))
                        .QueueToChannelAsync(e.Channel);

                    return;
                }
                await e.ErrorEmbedResource("error_language_invalid",
                    localeName,
                    await e.Prefix.GetForGuildAsync(context, cache, e.Guild.Id)
                ).ToEmbed().QueueToChannelAsync(e.Channel);
            }
        }

		[Command(Name = "setprefix", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task PrefixAsync(EventContext e)
		{
            var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));
            string args = e.Arguments.Pack.TakeAll();

            if (string.IsNullOrEmpty(args))
			{
                await e.ErrorEmbed(e.Locale.GetString("miki_module_general_prefix_error_no_arg")).ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			PrefixInstance defaultInstance = e.commandHandler.GetDefaultPrefix();

			using (var context = new MikiContext())
			{
				await defaultInstance.ChangeForGuildAsync(context, cache, e.Guild.Id, args);
			}

			EmbedBuilder embed = new EmbedBuilder();
			embed.SetTitle(e.Locale.GetString("miki_module_general_prefix_success_header"));
			embed.SetDescription(
				e.Locale.GetString("miki_module_general_prefix_success_message", args
            ));

			await embed.ToEmbed().QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "syncavatar")]
		public async Task SyncAvatarAsync(EventContext e)
		{
			await Utils.SyncAvatarAsync(e.Author);

			await e.SuccessEmbed(
				e.Locale.GetString("setting_avatar_updated")	
			).QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "listlocale", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task ListLocaleAsync(EventContext e)
		{
            await new EmbedBuilder()
			{
				Title = e.Locale.GetString("locales_available"),
				Description = ("`" + string.Join("`, `", Locale.LocaleNames.Keys) + "`")
			}.AddField(
				"Your language not here?",
				e.Locale.GetString("locales_contribute",
					$"[{e.Locale.GetString("locales_translations")}](https://poeditor.com/join/project/FIv7NBIReD)"
				)
			).ToEmbed().QueueToChannelAsync(e.Channel);
		}
	}
}