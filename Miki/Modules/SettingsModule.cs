using Discord;
using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module(Name = "settings")]
    class SettingsModule
    {
        [Command(Name = "toggledm")]
        public async Task ToggleDmAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Setting setting = await context.Settings.FindAsync(e.Author.Id.ToDbLong(), DatabaseEntityType.USER, DatabaseSettingId.PERSONALMESSAGE);

                if (setting == null)
                {
                    setting = context.Settings.Add(new Setting() { EntityId = e.Author.Id.ToDbLong(), EntityType = DatabaseEntityType.USER, IsEnabled = true, SettingId = DatabaseSettingId.PERSONALMESSAGE });
                }

                EmbedBuilder embed = new EmbedBuilder();
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                setting.IsEnabled = !setting.IsEnabled;
                string aa = (!setting.IsEnabled) ? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                embed.Description = locale.GetString("miki_module_settings_dm", aa);
                embed.Color = (setting.IsEnabled) ? new Discord.Color(1, 0, 0) : new Discord.Color(0, 1, 0);

                await context.SaveChangesAsync();
                await e.Channel.SendMessage(new RuntimeEmbed(embed));
            }
        }

        [Command(Name = "toggleerrors")]
        public async Task ToggleErrors(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Setting setting = await context.Settings.FindAsync(e.Author.Id.ToDbLong(), DatabaseEntityType.USER, DatabaseSettingId.ERRORMESSAGE);

                if (setting == null)
                {
                    setting = context.Settings.Add(new Setting() { EntityId = e.Author.Id.ToDbLong(), EntityType = DatabaseEntityType.USER, IsEnabled = true, SettingId = DatabaseSettingId.ERRORMESSAGE });
                }

                EmbedBuilder embed = new EmbedBuilder();
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                setting.IsEnabled = !setting.IsEnabled;

                string aa = (!setting.IsEnabled) ? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                embed.Description = locale.GetString("miki_module_settings_error_dm", aa);
                embed.Color = (setting.IsEnabled) ? new Discord.Color(1, 0, 0) : new Discord.Color(0, 1, 0);

                await context.SaveChangesAsync();
                await e.Channel.SendMessage(new RuntimeEmbed(embed));
            }
        }

        [Command(Name = "toggleguildnotifications", Aliases = new string[] { "tgn" }, Accessibility = EventAccessibility.ADMINONLY)]
        public async Task ToggleGuildNotifications(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Setting setting = await context.Settings.FindAsync(e.Guild.Id.ToDbLong(), DatabaseEntityType.GUILD, DatabaseSettingId.CHANNELMESSAGE);

                if (setting == null)
                {
                    setting = context.Settings.Add(new Setting() { EntityId = e.Guild.Id.ToDbLong(), EntityType = DatabaseEntityType.GUILD, IsEnabled = true, SettingId = DatabaseSettingId.CHANNELMESSAGE });
                }

                EmbedBuilder embed = new EmbedBuilder();
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                setting.IsEnabled = !setting.IsEnabled;

                string aa = (!setting.IsEnabled) ? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                embed.Description = locale.GetString("miki_module_settings_guild_notifications", aa);
                embed.Color = (setting.IsEnabled) ? new Discord.Color(1, 0, 0) : new Discord.Color(0, 1, 0);

                await context.SaveChangesAsync();
                await e.Channel.SendMessage(new RuntimeEmbed(embed));
            }
        }

        [Command(Name = "setlocale", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SetLocale(EventContext e)
        {
            using (var context = new MikiContext())
            {
                ChannelLanguage language = await context.Languages.FindAsync(e.Guild.Id.ToDbLong());
                Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

                if (!Locale.Locales.ContainsKey(e.arguments.ToLower()))
                {
                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "{0} is not a valid language. use `>help setlocale` to see all of the memes xd"));
                    return;
                }

                if (language == null)
                {
                    language = context.Languages.Add(new ChannelLanguage() { EntityId = e.Guild.Id.ToDbLong(), Language = e.arguments.ToLower() });
                }

                language.Language = e.arguments.ToLower();
                await e.Channel.SendMessage(Utils.SuccessEmbed(locale, "Set locale to `{0}`\n\n**WARNING:** this feature is not fully implemented yet. use at your own risk."));
                await context.SaveChangesAsync();
            }
        }

        [Command(Name = "setprefix", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task PrefixAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (string.IsNullOrEmpty(e.arguments))
            {
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, locale.GetString("miki_module_general_prefix_error_no_arg")));
                return;
            }

            await PrefixInstance.Default.ChangeForGuildAsync(e.Guild.Id, e.arguments);

            IDiscordEmbed embed = Utils.Embed;
            embed.Title = locale.GetString("miki_module_general_prefix_success_header");
            embed.Description = locale.GetString("miki_module_general_prefix_success_message", e.arguments);

            embed.AddField(f =>
            {
                f.Name = locale.GetString("miki_module_general_prefix_example_command_header");
                f.Value = $"{e.arguments}profile";
            });

            await e.Channel.SendMessage(embed);
        }

        [Command(Name = "listlocale", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task DoListLocale(EventContext e)
        {
            await Utils.Embed
                .SetTitle("Available locales")
                .SetDescription("`" + string.Join("`, `", Locale.Locales.Keys) + "`")
                .SendToChannel(e.Channel.Id);
        }
    }
}
