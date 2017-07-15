using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.API.UrbanDictionary;
using Miki.Languages;
using Miki.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("General")]
    class GeneralModule
    {
        public GeneralModule(RuntimeModule m)
        {
            Bot.instance.Events.AddCommandDoneEvent(x =>
            {
                x.Name = "--count-commands";
                x.processEvent = async (msg, e, s) =>
                {
                    if (s)
                    {
                        using (var context = new MikiContext())
                        {
                            CommandUsage u = await context.CommandUsages.FindAsync(msg.Author.Id.ToDbLong(), e.Name);
                            if (u == null)
                            {
                                u = context.CommandUsages.Add(new CommandUsage() { UserId = msg.Author.Id.ToDbLong(), Amount = 1, Name = e.Name });
                            }
                            else
                            {
                                u.Amount++;
                            }

                            User user = await context.Users.FindAsync(msg.Author.Id.ToDbLong());
                            user.Total_Commands++;

                            await context.SaveChangesAsync();
                        }
                    }
                };
            });
        }

        [Command(Name = "avatar")]
        public async Task AvatarAsync(EventContext e)
        {
            if (e.message.MentionedUserIds.Count > 0)
            {
                await e.Channel.SendMessage(string.Join(".", (await e.Guild.GetUserAsync(e.message.MentionedUserIds.First())).AvatarUrl));
            }
            else
            {
                await e.Channel.SendMessage(string.Join(".", e.Author.AvatarUrl));
            }
        }

        [Command(Name = "avatar", On = "-s")]
        public async Task ServerAvatarAsync(EventContext e)
        {
            await e.Channel.SendMessage(string.Join(".", e.Guild.AvatarUrl));
        }

        [Command(Name = "whois")]
        public async Task WhoIsAsync(EventContext e)
        {
            ulong id = 0;

            if (string.IsNullOrEmpty(e.arguments))
            {
                id = e.Author.Id;
            }
            else if (e.message.MentionedUserIds.Count == 0)
            {
                id = ulong.Parse(e.arguments);
            }
            else
            {
                id = e.message.MentionedUserIds.First();
            }

            IDiscordUser user = await e.Guild.GetUserAsync(id);

            Locale l = Locale.GetEntity(e.Channel.Id.ToDbLong());

            IDiscordEmbed embed = Utils.Embed;
            embed.Title = $"Who is {(string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname)}!?";
            embed.Color = new Color(0.5f, 0, 1);

            embed.ImageUrl = (await e.Guild.GetUserAsync(id)).AvatarUrl;

            embed.AddInlineField(
                l.GetString("miki_module_whois_tag_personal"),
                $"User Id      : **{user.Id}**\nUsername: **{user.Username}#{user.Discriminator} {(string.IsNullOrEmpty(user.Nickname) ? "" : $"({user.Nickname})")}**\nCreated at: **{user.CreatedAt.ToString()}**\nJoined at   : **{user.JoinedAt.ToString()}**\n");

            List<string> roles = new List<string>();
            foreach (ulong i in user.RoleIds)
            {
                roles.Add("`" + user.Guild.GetRole(i).Name + "`");
            }

            embed.AddInlineField(
                l.GetString("miki_module_general_guildinfo_roles"),
                string.Join(" ", roles));

            await e.Channel.SendMessage(embed);
        }

        [Command(Name = "calc", Aliases = new string[] { "calculate" })]
        public async Task CalculateAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            try
            {
                var result = new DataTable().Compute(e.arguments, null);
                await e.Channel.SendMessage(result.ToString());
            }
            catch
            {
                await e.Channel.SendMessage(locale.GetString("miki_module_general_calc_error"));
            }
        }

        [Command(Name = "guildinfo")]
        public async Task GuildInfoAsync(EventContext e)
        {
            IDiscordEmbed embed = Utils.Embed;
            Locale l = Locale.GetEntity(e.Channel.Id.ToDbLong());

            embed.SetAuthor(e.Guild.Name, e.Guild.AvatarUrl, e.Guild.AvatarUrl);

            embed.AddInlineField(
                "👑" + l.GetString("miki_module_general_guildinfo_owned_by"),
                e.Guild.Owner.Username + "#" + e.Guild.Owner.Discriminator);

            embed.AddInlineField(
                "👉" + l.GetString("miki_label_prefix"),
                await PrefixInstance.Default.GetForGuildAsync(e.Guild.Id));

            embed.AddInlineField(
                "📺" + l.GetString("miki_module_general_guildinfo_channels"),
                e.Guild.ChannelCount.ToString());

            embed.AddInlineField(
                "🔊" + l.GetString("miki_module_general_guildinfo_voicechannels"),
                e.Guild.VoiceChannelCount.ToString());

            embed.AddInlineField(
                "🙎" + l.GetString("miki_module_general_guildinfo_users"),
                e.Guild.UserCount.ToString());

            List<string> roleNames = new List<string>();
            foreach (IDiscordRole r in e.Guild.Roles)
            {
                roleNames.Add($"`{r.Name}`");
            }

            embed.AddInlineField(
                "#⃣" + l.GetString("miki_module_general_guildinfo_roles_count"),
                e.Guild.Roles.Count.ToString());

            embed.AddInlineField(
                "📜" + l.GetString("miki_module_general_guildinfo_roles"),
                string.Join(", ", roleNames));

            await e.Channel.SendMessage(embed);
        }

        [Command(Name = "help")]
        public async Task HelpAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (!string.IsNullOrEmpty(e.arguments))
            {
                ICommandEvent ev = Bot.instance.Events.CommandHandler.GetCommandEvent(e.arguments);
                if (ev == null)
                {
                    IDiscordEmbed helpListEmbed = Utils.Embed;
                    helpListEmbed.Title = locale.GetString("miki_module_help_error_null_header");
                    helpListEmbed.Description = locale.GetString("miki_module_help_error_null_message");
                    helpListEmbed.Color = new Color(1.0f, 0, 0);

                    bool done = false;

                    foreach (IModule a in Bot.instance.Events.Modules.Values)
                    {
                        foreach (ICommandEvent c in a.Events)
                        {
                            if (Bot.instance.Events.CommandHandler.GetUserAccessibility(e.message) < c.Accessibility)
                            {
                                continue;
                            }

                            if (done)
                            {
                                break;
                            }

                            if (c.Name.Contains(e.arguments))
                            {
                                helpListEmbed.AddField(f =>
                                {
                                    f.Name = locale.GetString("miki_module_help_didyoumean");
                                    f.Value = c.Name;
                                });
                                done = true;
                                break;
                            }

                            foreach (string alias in c.Aliases)
                            {
                                if (alias.Contains(e.arguments))
                                {
                                    helpListEmbed.AddField(f =>
                                    {
                                        f.Name = locale.GetString("miki_module_help_didyoumean");
                                        f.Value = c.Name;
                                    });
                                    done = true;
                                    break;
                                }
                            }
                        }

                        if (done)
                        {
                            break;
                        }
                    }

                    await e.Channel.SendMessage(helpListEmbed);
                }
                else
                {

                    if (Bot.instance.Events.CommandHandler.GetUserAccessibility(e.message) < ev.Accessibility)
                    {
                        return;
                    }

                    IDiscordEmbed explainedHelpEmbed = Utils.Embed
                        .SetTitle(ev.Name.ToUpper());

                    if (ev.Aliases.Length > 0)
                    {
                        explainedHelpEmbed.AddInlineField(
                            locale.GetString("miki_module_general_help_aliases"),
                            string.Join(", ", ev.Aliases));
                    }


                    explainedHelpEmbed.AddField(
                        locale.GetString("miki_module_general_help_description"),
                        (locale.HasString("miki_command_description_" + ev.Name.ToLower())) ? locale.GetString("miki_command_description_" + ev.Name.ToLower()) : locale.GetString("miki_placeholder_null"));

                    explainedHelpEmbed.AddField(
                        locale.GetString("miki_module_general_help_usage"),
                        (locale.HasString("miki_command_usage_" + ev.Name.ToLower())) ? locale.GetString("miki_command_usage_" + ev.Name.ToLower()) : locale.GetString("miki_placeholder_null"));

                    await e.Channel.SendMessage(explainedHelpEmbed);
                }
                return;
            }
            IDiscordEmbed embed = Utils.Embed;

            embed.Description = locale.GetString("miki_module_general_help_dm");

            embed.Color = new Color(0, 0.5f, 1);

            await e.Channel.SendMessage(embed);

            await e.Author.SendMessage(await Bot.instance.Events.ListCommandsInEmbedAsync(e.message));
        }

        [Command(Name = "info", Aliases = new string[] { "about" })]
        public async Task InfoAsync(EventContext e)
        {
            IDiscordEmbed embed = Utils.Embed;
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            embed.Author = embed.CreateAuthor();
            embed.Author.Name = "Miki " + Bot.instance.Version;
            embed.Color = new Color(1, 0.6f, 0.6f);


            embed.AddField(f =>
            {
                f.Name = locale.GetString("miki_module_general_info_made_by_header");
                f.Value = locale.GetString("miki_module_general_info_made_by_description");
            });

            embed.AddField(f =>
            {
                f.Name = "Links";
                f.Value =
                $"**{locale.GetString("miki_module_general_info_docs")}:** https://www.github.com/velddev/miki/wiki \n" +
                $"**{locale.GetString("miki_module_general_info_patreon")}:** https://www.patreon.com/mikibot \n" +
                $"**{locale.GetString("miki_module_general_info_twitter")}:** https://www.twitter.com/velddev / https://www.twitter.com/miki_discord \n" +
                $"**{locale.GetString("miki_module_general_info_reddit")}:** https://www.reddit.com/r/mikibot \n" +
                $"**{locale.GetString("miki_module_general_info_server")}:** https://discord.gg/55sAjsW \n" +
                $"**{locale.GetString("miki_module_general_info_website")}:** http://miki.veld.one";
            });

            await e.Channel.SendMessage(embed);
        }

        [Command(Name = "donate", Aliases = new string[] { "patreon" })]
        public async Task DonateAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
            await e.Channel.SendMessage(locale.GetString("miki_module_general_info_donate_string") + " <https://www.patreon.com/mikibot>");
        }

        [Command(Name = "ping")]
        public async Task PingAsync(EventContext e)
        {
            IDiscordMessage message = await Utils.Embed
                .SetTitle("Ping")
                .SetDescription("Hold on, pinging services...")
                .SendToChannel(e.Channel);

            if (message != null)
            {
                double ping = (message.Timestamp - e.message.Timestamp).TotalMilliseconds;

                await message.ModifyAsync(
                    Utils.Embed
                        .SetTitle("Pong")
                        .SetColor(Color.Lerp(new Color(0, 1, 0), new Color(1, 0, 0), (float)ping / 1000))
                        .AddInlineField("Miki", ping + "ms")
                        .AddInlineField("Discord", Bot.instance.Client.Latency + "ms"));
            }
        }

        [Command(Name = "invite")]
        public async Task InviteAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
            Locale authorLocale = Locale.GetEntity(e.Author.Id.ToDbLong());

            await e.Channel.SendMessage(locale.GetString("miki_module_general_invite_message"));

            await e.Author.SendMessage(authorLocale.GetString("miki_module_general_invite_dm") 
                + "\nhttps://discordapp.com/oauth2/authorize?&client_id=160185389313818624&scope=bot&permissions=355593334");
        }

        [Command(Name = "prefix", Accessibility = EventAccessibility.ADMINONLY, On = "?")]
        public async Task PrefixHelpAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            await Utils.Embed
                .SetTitle(locale.GetString("miki_module_general_prefix_help_header"))
                .SetDescription(locale.GetString("miki_module_general_prefix_help", await PrefixInstance.Default.GetForGuildAsync(e.Guild.Id)))
                .SendToChannel(e.Channel.Id);
        }

        [Command(Name = "stats")]
        public async Task StatsAsync(EventContext e)
        {
            //int servers = bot.Client.Guilds.Count;
            //int channels = bot.Client.Guilds.Sum(a => a.Channels.Count);
            //int members = bot.Client.Guilds.Sum(a => a.Channels.Sum(b => b.Users.Count));

            TimeSpan timeSinceStart = DateTime.Now.Subtract(Program.timeSinceStartup);

            IDiscordEmbed embed = Utils.Embed;
            embed.Title = "⚙️ Miki stats";
            embed.Description = "General realtime stats about miki!";
            embed.Color = new IA.SDK.Color(0.3f, 0.8f, 1);

            //embed.AddField(f =>
            //{
            //    f.Name = "🖥️ Servers";
            //    f.Value = servers.ToString();
            //    f.IsInline = true;
            //});

            //embed.AddField(f =>
            //{
            //    f.Name = "📺 Channels";
            //    f.Value = channels.ToString();
            //    f.IsInline = true;
            //});

            //embed.AddField(f =>
            //{
            //    f.Name = "👤 Users";
            //    f.Value = members.ToString();
            //    f.IsInline = true;
            //});

            //embed.AddField(f =>
            //{
            //    f.Name = "🐏 Ram";
            //    f.Value = (memsize / 1024 / 1024).ToString() + "MB";
            //    f.IsInline = true;
            //});

            //embed.AddField(f =>
            //{
            //    f.Name = "👷 Threads";
            //    f.Value = threads.ToString();
            //    f.IsInline = true;
            //});

            embed.AddField(f =>
            {
                f.Name = "💬 Commands";
                f.Value = Bot.instance.Events.CommandsUsed().ToString();
                f.IsInline = true;
            });

            embed.AddField(f =>
            {
                f.Name = "⏰ Uptime";
                f.Value = timeSinceStart.ToTimeString();
                f.IsInline = true;
            });

            await e.Channel.SendMessage(embed);
        }

        [Command(Name = "urban")]
        public async Task UrbanAsync(EventContext e)
        {
            if (string.IsNullOrEmpty(e.arguments)) return;

            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
            UrbanDictionaryApi api = new UrbanDictionaryApi(Global.UrbanKey);
            UrbanDictionaryEntry entry = api.GetEntry(e.arguments);

            if (entry != null)
            {
                IDiscordEmbed embed = Utils.Embed
                    .SetAuthor(entry.Term,
                        "http://cdn9.staztic.com/app/a/291/291148/urban-dictionary-647813-l-140x140.png",
                        "http://www.urbandictionary.com/define.php?term=" + e.arguments)
                    .SetDescription(locale.GetString("miki_module_general_urban_author", entry.Author));

                embed.AddInlineField(locale.GetString("miki_module_general_urban_definition"), entry.Definition);
                embed.AddInlineField(locale.GetString("miki_module_general_urban_example"), entry.Example);
                embed.AddInlineField(locale.GetString("miki_module_general_urban_rating"), "👍 " + entry.ThumbsUp + "  👎 " + entry.ThumbsDown);

                await e.Channel.SendMessage(embed);
            }
            else
            {
                await Utils.ErrorEmbed(locale, "This term couldn't been found!")
                    .SendToChannel(e.Channel.Id);
            }
        }
    }
}
