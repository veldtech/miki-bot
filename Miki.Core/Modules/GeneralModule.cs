using Discord;
using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.API.UrbanDictionary;
using Miki.Languages;
using Miki.Models;
using NCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("General")]
    internal class GeneralModule
    {
        public GeneralModule(RuntimeModule m)
        {
            Bot.instance.Events.AddCommandDoneEvent(x =>
            {
                x.Name = "--count-commands";
                x.processEvent = async (msg, e, s, t) =>
                {
                    if (s)
                    {
                        using (var context = new MikiContext())
                        {
                            CommandUsage u = await context.CommandUsages.FindAsync(msg.Author.Id.ToDbLong(), e.Name);
                            if (u == null)
                            {
                                u = context.CommandUsages.Add(new CommandUsage() { UserId = msg.Author.Id.ToDbLong(), Amount = 1, Name = e.Name }).Entity;
                            }
                            else
                            {
                                u.Amount++;
                            }

                            User user = await context.Users.FindAsync(msg.Author.Id.ToDbLong());
                            if(user != null)
                            {
                                user.Total_Commands++;
                            }

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
                await e.Channel.QueueMessageAsync(string.Join(".", (await e.Guild.GetUserAsync(e.message.MentionedUserIds.First())).AvatarUrl));
            }
            else
            {
                await e.Channel.QueueMessageAsync(string.Join(".", e.Author.AvatarUrl));
            }	
        }

        [Command(Name = "avatar", On = "-s")]
        public async Task ServerAvatarAsync(EventContext e)
        {
            await e.Channel.QueueMessageAsync(string.Join(".", e.Guild.AvatarUrl));
        }

        [Command(Name = "calc", Aliases = new string[] { "calculate" })]
        public async Task CalculateAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

			try
			{
                Expression expression = new Expression(e.arguments);

                expression.Parameters.Add("pi", Math.PI);

                expression.EvaluateFunction += (name, x) =>
                {
                    if (name == "lerp")
                    {
                        double n = (double)x.Parameters[0].Evaluate();
                        double v = (double)x.Parameters[1].Evaluate();
                        double o = (double)x.Parameters[2].Evaluate();
                        x.Result = (n * (1.0 - o)) + (v * o);
                    }
                };

                object output = expression.Evaluate();

                await e.Channel.QueueMessageAsync(output.ToString());
            }
            catch(Exception ex)
            {
                await e.Channel.QueueMessageAsync(locale.GetString("miki_module_general_calc_error") + "\n```" + ex.Message + "```");
            }
        }

		[Command(Name = "changelog")]
		public async Task ChangelogAsync(EventContext e)
		{
			await Utils.Embed
				.SetTitle("Change log")
				.SetDescription("Check out my changelog blog [here](https://medium.com/@velddev)")
				.QueueToChannel(e.Channel);
		}

        [Command(Name = "guildinfo")]
        public async Task GuildInfoAsync(EventContext e)
        {
            IDiscordEmbed embed = Utils.Embed;
            Locale l = Locale.GetEntity(e.Channel.Id.ToDbLong());

            embed.SetAuthor(e.Guild.Name, e.Guild.AvatarUrl, e.Guild.AvatarUrl);

			IDiscordUser owner = await e.Guild.GetOwnerAsync();

            embed.AddInlineField(
                "👑" + l.GetString("miki_module_general_guildinfo_owned_by"),
                owner.Username + "#" + owner.Discriminator);

            embed.AddInlineField(
                "👉" + l.GetString("miki_label_prefix"),
                await PrefixInstance.Default.GetForGuildAsync(e.Guild.Id));

            embed.AddInlineField(
                "📺" + l.GetString("miki_module_general_guildinfo_channels"),
                (await e.Guild.GetChannelCountAsync()).ToString());

            embed.AddInlineField(
                "🔊" + l.GetString("miki_module_general_guildinfo_voicechannels"),
                (await e.Guild.GetVoiceChannelCountAsync()).ToString());

            embed.AddInlineField(
                "🙎" + l.GetString("miki_module_general_guildinfo_users"),
                (await e.Guild.GetUserCountAsync()).ToString());

			embed.AddInlineField(
				"🤖" + l.GetString("term_shard"), 
				Bot.instance.Client.GetShardIdFor((e.Guild as IProxy<IGuild>).ToNativeObject()));

            List<string> roleNames = new List<string>();
            foreach (IDiscordRole r in e.Guild.Roles)
            {
                roleNames.Add($"`{r.Name}`");
            }

            embed.AddInlineField(
                "#⃣" + l.GetString("miki_module_general_guildinfo_roles_count"),
                e.Guild.Roles.Count.ToString());

			string roles = string.Join(", ", roleNames);

			if (roles.Length <= 1000)
			{
				embed.AddInlineField(
					"📜" + l.GetString("miki_module_general_guildinfo_roles"),
					roles);
			}
			
            await embed.QueueToChannel(e.Channel);
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
                    helpListEmbed.Description = locale.GetString("miki_module_help_error_null_message", await Bot.instance.Events.GetPrefixInstance(">").GetForGuildAsync(e.Guild.Id));
					helpListEmbed.SetColor(0.6f, 0.6f, 1.0f);

					API.StringComparison.StringComparer comparer = new API.StringComparison.StringComparer(e.commandHandler.GetAllEventNames());
                    API.StringComparison.StringComparison best = comparer.GetBest(e.arguments);

                    helpListEmbed.AddField(locale.GetString("miki_module_help_didyoumean"), best.text);

                    await helpListEmbed.QueueToChannel(e.Channel);
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

                    await explainedHelpEmbed.QueueToChannel(e.Channel);
                }
                return;
            }
            IDiscordEmbed embed = Utils.Embed;

            embed.Description = locale.GetString("miki_module_general_help_dm");

            embed.SetColor(0.6f, 0.6f, 1.0f);

			await embed.QueueToChannel(e.Channel);

            await (await Bot.instance.Events.ListCommandsInEmbedAsync(e.message)).QueueToUser(e.Author);
        }

        [Command(Name = "donate", Aliases = new string[] { "patreon" })]
        public async Task DonateAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
			await Utils.Embed
				.SetTitle("Hi everyone!")
				.SetDescription(e.GetResource("miki_module_general_info_donate_string"))
				.SetColor(0.8f, 0.4f, 0.4f)
				.SetThumbnailUrl("https://trello-attachments.s3.amazonaws.com/57acf354029527926a15e83d/598763ed8a7735cb8b52cd72/1d168f6025e40b9c6b53c3d4b8e07ccf/xdmemes.png")
				.AddInlineField("Links", "https://www.patreon.com/mikibot - if you want to donate every month and get cool rewards!\nhttps://ko-fi.com/velddy - one time donations please include your discord name#identifiers so i can contact you!")
				.AddInlineField("Don't have money?", "You can always support us in different ways too! Please participate in our [Trello](https://trello.com/b/SdjIVMtx/miki) discussion so we can get a better grasp of what you guys would like to see next! Or vote for Miki on [Discordbots.org](https://discordbots.org/bot/160105994217586689)")
				.QueueToChannel(e.Channel);
        }

        [Command(Name = "info", Aliases = new string[] { "about" })]
        public async Task InfoAsync(EventContext e)
        {
            IDiscordEmbed embed = Utils.Embed;
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            embed.Author = embed.CreateAuthor();
            embed.Author.Name = "Miki " + Bot.instance.Version;
			embed.SetColor(0.6f, 0.6f, 1.0f);

			embed.AddField(locale.GetString("miki_module_general_info_made_by_header"), 
				locale.GetString("miki_module_general_info_made_by_description") + " B., Drummss, Fuzen, IA, Luke, Milk, n0t, Phanrazak, Rappy, Tal, Vallode");


			embed.AddField(e.GetResource("miki_module_general_info_links"),
                $"`{locale.GetString("miki_module_general_info_docs").PadRight(15)}:` [documentation](https://www.github.com/velddev/miki/wiki)\n" +
                $"`{"donate".PadRight(15)}:` [patreon](https://www.patreon.com/mikibot) | [ko-fi](https://ko-fi.com/velddy)\n" +
                $"`{locale.GetString("miki_module_general_info_twitter").PadRight(15)}:` [veld](https://www.twitter.com/velddev) | [miki](https://www.twitter.com/miki_discord)\n" +
                $"`{locale.GetString("miki_module_general_info_reddit").PadRight(15)}:` [/r/mikibot](https://www.reddit.com/r/mikibot) \n" +
                $"`{locale.GetString("miki_module_general_info_server").PadRight(15)}:` [discord](https://discord.gg/55sAjsW)\n" +
                $"`{locale.GetString("miki_module_general_info_website").PadRight(15)}:` [link](https://miki.ai)");

            await embed.QueueToChannel(e.Channel);
        }

        [Command(Name = "invite")]	
        public async Task InviteAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
            Locale authorLocale = Locale.GetEntity(e.Author.Id.ToDbLong());

            await e.Channel.QueueMessageAsync(locale.GetString("miki_module_general_invite_message"));
            await e.Author.QueueMessageAsync(authorLocale.GetString("miki_module_general_invite_dm")
                + "\nhttps://discordapp.com/oauth2/authorize?&client_id=160185389313818624&scope=bot&permissions=355593334");
        }

        [Command(Name = "ping", Aliases = new string[] { "lag" })]
        public async Task PingAsync(EventContext e)
        {
            Task<IDiscordMessage> message = Utils.Embed
                .SetTitle("Ping")
                .SetDescription(e.GetResource("ping_placeholder"))
                .SendToChannel(e.Channel);

            IDiscordMessage returnedMessage = await message;

            await Task.Delay(100);

            if (returnedMessage != null)
            {
                double ping = (returnedMessage.Timestamp - e.message.Timestamp).TotalMilliseconds;

                await Utils.Embed
                    .SetTitle("Pong")
                    .SetColor(IA.SDK.Color.Lerp(new IA.SDK.Color(0, 1, 0), new IA.SDK.Color(1, 0, 0), (float)ping / 1000))
                    .AddInlineField("Miki", ping + "ms")
                    .AddInlineField("Discord", Bot.instance.Client.Latency + "ms")
                    .ModifyMessage(returnedMessage);
            }
        }

        [Command(Name = "prefix")]
        public async Task PrefixHelpAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            await Utils.Embed
                .SetTitle(locale.GetString("miki_module_general_prefix_help_header"))
                .SetDescription(locale.GetString("miki_module_general_prefix_help", await PrefixInstance.Default.GetForGuildAsync(e.Guild.Id)))
                .QueueToChannel(e.Channel.Id);
        }

        [Command(Name = "stats")]
        public async Task StatsAsync(EventContext e)
        {
            TimeSpan timeSinceStart = DateTime.Now.Subtract(Program.timeSinceStartup);

            IDiscordEmbed embed = Utils.Embed;
            embed.Title = "⚙️ Miki stats";
            embed.Description = e.GetResource("stats_description");
            embed.Color = new IA.SDK.Color(0.3f, 0.8f, 1);

            embed.AddInlineField($"🖥️ {e.GetResource("discord_servers")}", Bot.instance.Client.Guilds.Count.ToString());
            embed.AddInlineField("💬 " + e.GetResource("term_commands"), Bot.instance.Events.CommandsUsed().ToString());
            embed.AddInlineField("⏰ Uptime", timeSinceStart.ToTimeString(e.Channel.GetLocale()));
			embed.AddInlineField("More info", "https://p.datadoghq.com/sb/01d4dd097-08d1558da4");

            await embed.QueueToChannel(e.Channel);
        }

        [Command(Name = "urban")]
        public async Task UrbanAsync(EventContext e)
        {
            if (string.IsNullOrEmpty(e.arguments)) return;

            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
            UrbanDictionaryApi api = new UrbanDictionaryApi(Global.config.UrbanKey);
            UrbanDictionaryEntry entry = await api.GetEntryAsync(e.arguments);

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

                await embed.QueueToChannel(e.Channel);
            }
            else
            {
                await Utils.ErrorEmbed(locale, e.GetResource("error_term_invalid"))
                    .QueueToChannel(e.Channel.Id);
            }
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
            embed.SetColor(0.5f, 0f, 1.0f);

			embed.ImageUrl = (await e.Guild.GetUserAsync(id)).AvatarUrl;

            embed.AddInlineField(
                l.GetString("miki_module_whois_tag_personal"),
                $"User Id      : **{user.Id}**\nUsername: **{user.Username}#{user.Discriminator} {(string.IsNullOrEmpty(user.Nickname) ? "" : $"({user.Nickname})")}**\nCreated at: **{user.CreatedAt.ToString()}**\nJoined at   : **{user.JoinedAt.ToString()}**\n");

            List<string> roles = new List<string>();
            foreach (ulong i in user.RoleIds)
            {
                roles.Add("`" + user.Guild.GetRole(i).Name + "`");
            }

			string r = string.Join(" ", roles);
			if (r.Length <= 1000)
			{
				embed.AddInlineField(
					l.GetString("miki_module_general_guildinfo_roles"),
					r				
				);
			}
            await embed.QueueToChannel(e.Channel);
        }
    }
}