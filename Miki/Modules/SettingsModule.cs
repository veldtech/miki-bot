using Discord;
using IA;
using IA.Events;
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
    class SettingsModule
    {
        public async Task LoadEvents(Bot bot)
        {
            IModule module_settings = new Module(module =>
        {
            module.Name = "settings";
            module.Events = new List<ICommandEvent>()
                {
                    new CommandEvent(x =>
                    {
                        x.Name = "toggledm";
                        x.Metadata = new EventMetadata(
                            "Toggle Miki's ability to PM you.",
                            "I couldn't toggle that for you!",
                            ">togglepm");
                        x.ProcessCommand = async (e) =>
                        {
                            using(var context = new MikiContext())
                            {
                                Setting setting = await context.Settings.FindAsync(e.Author.Id.ToDbLong(), DatabaseEntityType.USER, DatabaseSettingId.PERSONALMESSAGE);

                                if(setting == null)
                                {
                                    setting = context.Settings.Add(new Setting(){ EntityId = e.Author.Id.ToDbLong(), EntityType = DatabaseEntityType.USER, IsEnabled = true, SettingId = DatabaseSettingId.PERSONALMESSAGE });
                                }

                                EmbedBuilder embed = new EmbedBuilder();
                                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                                setting.IsEnabled = !setting.IsEnabled;
                                string aa = (!setting.IsEnabled)? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                                embed.Description = locale.GetString("miki_module_settings_dm", aa);
                                embed.Color = (setting.IsEnabled)? new Discord.Color(1, 0 ,0) : new Discord.Color(0, 1, 0);

                                await context.SaveChangesAsync();
                                await e.Channel.SendMessage(new RuntimeEmbed(embed));
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "toggleguildnotifications";
                        x.Aliases = new string[]{"tgn"};
                        x.Accessibility = EventAccessibility.ADMINONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            using(var context = new MikiContext())
                            {
                                Setting setting = await context.Settings.FindAsync(e.Guild.Id.ToDbLong(), DatabaseEntityType.GUILD, DatabaseSettingId.CHANNELMESSAGE);

                                if(setting == null)
                                {
                                    setting = context.Settings.Add(new Setting(){ EntityId = e.Guild.Id.ToDbLong(), EntityType = DatabaseEntityType.GUILD, IsEnabled = true, SettingId = DatabaseSettingId.CHANNELMESSAGE });
                                }

                                EmbedBuilder embed = new EmbedBuilder();
                                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                                setting.IsEnabled = !setting.IsEnabled;

                                string aa = (!setting.IsEnabled)? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                                embed.Description = locale.GetString("miki_module_settings_guild_notifications", aa);
                                embed.Color = (setting.IsEnabled)? new Discord.Color(1, 0 ,0) : new Discord.Color(0, 1, 0);

                                await context.SaveChangesAsync();
                                await e.Channel.SendMessage(new RuntimeEmbed(embed));
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "toggleerrors";
                        x.ProcessCommand = async (e) =>
                        {
                            using(var context = new MikiContext())
                            {
                                Setting setting = await context.Settings.FindAsync(e.Author.Id.ToDbLong(), DatabaseEntityType.USER, DatabaseSettingId.ERRORMESSAGE);

                                if(setting == null)
                                {
                                    setting = context.Settings.Add(new Setting(){ EntityId = e.Author.Id.ToDbLong(), EntityType = DatabaseEntityType.USER, IsEnabled = true, SettingId = DatabaseSettingId.ERRORMESSAGE });
                                }

                                EmbedBuilder embed = new EmbedBuilder();
                                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                                                                setting.IsEnabled = !setting.IsEnabled;

                                string aa = (!setting.IsEnabled)? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                                embed.Description = locale.GetString("miki_module_settings_error_dm", aa);
                                embed.Color = (setting.IsEnabled)? new Discord.Color(1, 0 ,0) : new Discord.Color(0, 1, 0);

                                await context.SaveChangesAsync();
                                await e.Channel.SendMessage(new RuntimeEmbed(embed));
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "setlocale";
                        x.Accessibility = EventAccessibility.ADMINONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            using(var context = new MikiContext())
                            {
                                ChannelLanguage language = await context.Languages.FindAsync(e.Guild.Id.ToDbLong());
                                Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

                                if(!Locale.Locales.ContainsKey(e.arguments.ToLower()))
                                {
                                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "{0} is not a valid language. use `>help setlocale` to see all of the memes xd"));
                                    return;
                                }

                                if(language == null)
                                {
                                    language = context.Languages.Add(new ChannelLanguage(){ EntityId = e.Guild.Id.ToDbLong(), Language = e.arguments.ToLower() });
                                }

                                language.Language = e.arguments.ToLower();
                                await e.Channel.SendMessage(Utils.SuccessEmbed(locale, "Set locale to `{0}`\n\n**WARNING:** this feature is not fully implemented yet. use at your own risk."));
                                await context.SaveChangesAsync();
                            }
                        };
                    }).On("list", DoListLocale)
                };
            });

            await new RuntimeModule(module_settings).InstallAsync(bot);
        }

        public async Task DoListLocale(EventContext e)
        {
            await Utils.Embed
                .SetTitle("Available locales")
                .SetDescription("`" + string.Join("`, `", Locale.Locales.Keys) + "`")
                .SendToChannel(e.Channel.Id);
        }
    }
}
