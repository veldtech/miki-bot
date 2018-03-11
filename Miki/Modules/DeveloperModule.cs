using Discord;
using Discord.WebSocket;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Common.Events;
using Miki.Common.Interfaces;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("Experimental")]
    internal class DeveloperModule
    {
        public DeveloperModule(RuntimeModule module)
        {
        }

		[Command(Name = "identifyemoji", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task IdentifyEmojiAsync(EventContext e)
        {
            Emote emote = Emote.Parse(e.Arguments.ToString());

            Utils.Embed.SetTitle("Emoji Identified!")
                .AddInlineField("Name", emote.Name)
                .AddInlineField("Id", emote.Id.ToString())
                .AddInlineField("Created At", emote.CreatedAt.ToString())
                .AddInlineField("Code", "`" + emote.ToString() + "`")
                .SetThumbnailUrl(emote.Url)
                .QueueToChannel(e.Channel);

			await Task.Yield();
		}

		[Command(Name = "say", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task SayAsync(EventContext e)
        {
            e.Channel.QueueMessageAsync(e.Arguments.ToString());
        }

        [Command(Name = "sayembed", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task SayEmbedAsync(EventContext e)
        {
            Utils.Embed.AddInlineField("SAY", e.Arguments.ToString())
				.QueueToChannel(e.Channel);

			await Task.Yield();
		}

        [Command(Name = "setgame", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task SetGameAsync(EventContext e)
        {
            await e.message.Discord.SetGameAsync(e.Arguments.ToString());
        }

        [Command(Name = "setstream", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task SetStreamAsync(EventContext e)
        {
            await e.message.Discord.SetGameAsync(e.Arguments.ToString(), "https://www.twitch.tv/velddev");
        }

        [Command(Name = "ignore", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task IgnoreIdAsync(EventContext e)
        {
            if (ulong.TryParse(e.Arguments.ToString(), out ulong id))
            {
                EventSystem.Instance.Ignore(id);
                e.Channel.QueueMessageAsync(":ok_hand:");
            }
        }

        [Command(Name = "dev", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task ShowCacheAsync(EventContext e)
        {
            e.Channel.QueueMessageAsync("Yes, this is Veld, my developer.");
        }

        [Command(Name = "qembed", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task QueryEmbedAsync(EventContext e)
        {
            new RuntimeEmbed().Query(e.Arguments.Join().Argument)
				.QueueToChannel(e.Channel);

			await Task.Yield();
        }

        [Command(Name = "changeavatar", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task ChangeAvatarAsync(EventContext e)
        {
			using (Stream s = new FileStream("./" + e.Arguments.Join(), FileMode.Open))
			{
				await Bot.Instance.GetShard(e.message.Discord.ShardId).CurrentUser.ModifyAsync(z =>
				{
					z.Image = new DiscordImage(s);
				});
			}
        }

        [Command(Name = "dumpshards", Accessibility = EventAccessibility.DEVELOPERONLY, Aliases = new string[] { "ds" })]
        public async Task DumpShards(EventContext e)
        {
            IDiscordEmbed embed = Utils.Embed;
            embed.Title = "Shards";

			foreach (IShard c in Bot.Instance.Shards)
			{
				embed.Description += $"`Shard {c.Id.ToString().PadRight(2)}` | `State: {c.ConnectionState} Ping: {c.Latency} Guilds: {c.Guilds.Count}`";
            }

            embed.QueueToChannel(e.Channel);

			await Task.Yield();
        }

        [Command(Name = "spellcheck", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task SpellCheckAsync(EventContext context)
        {
            IDiscordEmbed embed = Utils.Embed;

            embed.SetTitle("Spellcheck - top results");

            API.StringComparison.StringComparer sc = new API.StringComparison.StringComparer(context.commandHandler.GetAllEventNames());
            List<API.StringComparison.StringComparison> best = sc.CompareToAll(context.Arguments.ToString())
                                                                 .OrderBy(z => z.score)
                                                                 .ToList();
            int x = 1;

            foreach (API.StringComparison.StringComparison c in best)
            {
                embed.AddInlineField($"#{x}", c);
                x++;
                if (x > 16) break;
            }

            embed.QueueToChannel(context.Channel);

			await Task.Yield();
        }

        [Command(Name = "setdonator", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task SetDonator(EventContext context)
        {
            using (MikiContext database = new MikiContext())
            {
                if (context.message.MentionedUserIds.Count > 0)
                {
                    Achievement a = await database.Achievements.FindAsync(context.message.MentionedUserIds.First().ToDbLong(), "donator");
                    if (a == null)
                    {
                        database.Achievements.Add(new Achievement() { Id = context.message.MentionedUserIds.First().ToDbLong(), Name = "donator", Rank = 0 });
                        await database.SaveChangesAsync();
                    }
                }
                else
                {
                    ulong.TryParse(context.message.Content, out ulong x);
                    if (x != 0)
                    {
                        database.Achievements.Add(new Achievement() { Id = x.ToDbLong(), Name = "donator", Rank = 0 });
                        await database.SaveChangesAsync();
                    }
                }
                context.Channel.QueueMessageAsync(":ok_hand:");
            }
        }

        [Command(Name = "setmekos", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task SetMekos(EventContext e)
        {
			ArgObject arg = e.Arguments.FirstOrDefault();

			IDiscordUser user = await arg.GetUserAsync(e.Guild);

			arg = arg.Next();

			int amount = arg?.AsInt(0) ?? 0;

            using (var context = new MikiContext())
            {
                User u = await context.Users.FindAsync((long)user.Id);
                if (u == null)
                {
                    return;
                }
				u.Currency = amount;
                await context.SaveChangesAsync();
                e.Channel.QueueMessageAsync(":ok_hand:");
            }
        }

        [Command(Name = "finduserbyid", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task FindUserById(EventContext e)
        {
            IDiscordUser u = Bot.Instance.GetUser(ulong.Parse(e.Arguments.ToString()));

            e.Channel.QueueMessageAsync(u.Username + "#" + u.Discriminator);
        }

		[Command(Name = "createkey", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task CreateKeyAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				DonatorKey key = (await context.DonatorKey.AddAsync(new DonatorKey()
				{
					StatusTime = new TimeSpan(int.Parse(e.Arguments.ToString()), 0, 0, 0, 0)
				})).Entity;

				await context.SaveChangesAsync();
				e.Channel.QueueMessageAsync($"key generated for {e.Arguments.ToString()} days `{key.Key}`");
			}
		}

		[Command(Name = "setexp", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task SetExp(EventContext e)
        {
			ArgObject arg = e.Arguments.FirstOrDefault();

			IDiscordUser user = await arg.GetUserAsync(e.Guild);

			arg = arg.Next();

			int amount = arg?.AsInt(0) ?? 0;

			using (var context = new MikiContext())
            {
                LocalExperience u = await LocalExperience.GetAsync(context, e.Guild.Id.ToDbLong(), user);
                if (u == null)
                {
                    return;
                }
                u.Experience = amount;
                await context.SaveChangesAsync();
				await Global.redisClient.AddAsync($"user:{e.Guild.Id}:{e.Author.Id}:exp", u.Experience);
				e.Channel.QueueMessageAsync(":ok_hand:");
            }
        }

		[Command(Name = "banuser", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task BanUserAsync(EventContext e)
		{
			await User.BanAsync(long.Parse(e.Arguments.ToString()));
		}
    }
}