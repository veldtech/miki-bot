using Discord;
using Discord.WebSocket;
using IA;
using IA.Events;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Extensions;
using IA.SDK.Interfaces;
using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.Languages;
using Miki.Models;
using Miki.Models.Objects.Guild;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    internal class DeveloperModule
    {
        public async Task LoadEvents(Bot bot)
        {
            // TODO: Change to SDK
            await new RuntimeModule(module =>
            {
                module.Name = "Experimental";
                module.Events = new List<ICommandEvent>()
                {
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "dumpshards";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            EmbedBuilder embed = new EmbedBuilder();
                            embed.Title = "Shards";

                            foreach(DiscordSocketClient c in bot.Client.Shards)
                            {
                                embed.AddField(f =>
                                {
                                    f.Name = "Shard " + c.ShardId;
                                    f.Value = $"State:  {c.ConnectionState}\nPing:   {c.Latency}\nGuilds: {c.Guilds.Count}";
                                    f.IsInline = true;
                                });
                            }

                            await e.Channel.SendMessage(new RuntimeEmbed(embed));
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "changeavatar";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            Image s = new Image(new FileStream("./" + e.arguments, FileMode.Open));

                            await bot.Client.GetShard(e.message.Discord.ShardId).CurrentUser.ModifyAsync(z =>
                            {
                                z.Avatar = new Optional<Image?>(s);
                            });
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "changename";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            await bot.Client.GetShard(e.message.Discord.ShardId).CurrentUser.ModifyAsync(z =>
                             {
                                 z.Username = e.arguments;
                             });
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "setexp";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            using(var context = new MikiContext())
                            {
                                LocalExperience u = await context.Experience.FindAsync(e.Guild.Id.ToDbLong(), e.message.MentionedUserIds.First().ToDbLong());
                                if(u == null)
                                {
                                    return;
                                }
                                u.Experience = int.Parse(e.arguments.Split(' ')[1]);
                                await context.SaveChangesAsync();
                            }
                        };
                    }),
                    new RuntimeCommandEvent("setdonator")
                        .SetAccessibility(EventAccessibility.DEVELOPERONLY)
                        .Default(DoSetDonator),
                    new RuntimeCommandEvent("setmekos")
                        .SetAccessibility(EventAccessibility.DEVELOPERONLY)
                        .Default(DoSetMekos),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "setgame";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            await e.message.Discord.SetGameAsync(e.arguments);
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "setstream";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            await e.message.Discord.SetGameAsync(e.arguments, "https://www.twitch.tv/velddev");
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "testnotification";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            await Notification.SendPM(e.Author.Id, e.arguments);
                        };
                    }),
                    new RuntimeCommandEvent("commandsystemtest")
                        .SetAccessibility(EventAccessibility.DEVELOPERONLY)
                        .Default(async (e) =>
                        {
                            await Utils.Embed.SetColor(IA.SDK.Color.GetColor(IAColor.ORANGE)).SetDescription("This is the default command param").SendToChannel(e.Channel.Id);
                        })
                        .On("?", async (e) =>
                        {
                            await Utils.Embed.SetDescription("? was triggered").SendToChannel(e.Channel.Id);
                        })
                        .On("say", async (e) =>
                        {
                            await Utils.Embed.SetTitle("SAY").SetDescription(e.arguments).SendToChannel(e.Channel.Id);
                        }),
                    new RuntimeCommandEvent("guildprofile")
                        .Default(DoGuildProfile),
                    new RuntimeCommandEvent("guildnewrival")
                        .Default(DoGuildNewRival),
                    new RuntimeCommandEvent("guildweekly")
                        .Default(DoGuildWeekly),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "unload";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            await bot.Addons.Unload(bot, e.arguments);
                            await e.Channel.SendMessage($"Unloaded Add-On \"{e.arguments}\" successfully");
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "load";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            await bot.Addons.LoadSpecific(bot, e.arguments);
                            await e.Channel.SendMessage($"Loaded Add-On \"{e.arguments}\" successfully");
                        };
                    }),
                    new RuntimeCommandEvent("cmdtest")
                        .SetAccessibility(EventAccessibility.DEVELOPERONLY)
                        .Default(DoCmdTest),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "reload";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            await bot.Addons.Unload(bot, e.arguments);
                            await bot.Addons.LoadSpecific(bot, e.arguments);
                            await e.Channel.SendMessage($"Reloaded {e.arguments} successfully!");
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "queryembed";
                        x.ProcessCommand = async (e) =>
                        {
                            await e.Channel.SendMessage(new RuntimeEmbed(new EmbedBuilder()).Query(e.arguments));
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "mtou";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.ProcessCommand = async (e) =>
                        {
                            await e.Channel.SendMessage(e.message.RemoveMentions());
                        };
                    })
                };
            }).InstallAsync(bot);
        }

        private async Task DoGuildWeekly(EventContext context)
        {
            using (MikiContext database = new MikiContext())
            {
                GuildUser thisGuild = await database.GuildUsers.FindAsync(context.Guild.Id.ToDbLong());
                Timer timer = await database.Timers.FindAsync(context.Guild.Id.ToDbLong(), context.Author.Id.ToDbLong());

                if(timer == null)
                {
                    timer = new Timer()
                    {
                        GuildId = context.Guild.Id.ToDbLong(),
                        UserId = context.Author.Id.ToDbLong(),
                        Value = DateTime.MinValue
                    };
                    await database.SaveChangesAsync();
                }

                if (timer.Value.AddDays(7) <= DateTime.Now)
                {
                    SocketGuild guild = Bot.instance.Client.GetGuild(thisGuild.Id.FromDbLong());

                    GuildUser rival = await thisGuild.GetRival();

                    if (rival == null)
                    {
                        await Utils.Embed
                            .SetTitle("Weekly")
                            .SetDescription("!")
                            .SendToChannel(context.Channel);
                        return;
                    }

                    if (rival.Experience > thisGuild.Experience)
                    {
                        await Utils.Embed
                            .SetTitle("Weekly")
                            .SetDescription("you got to have a higher level than your rival!")
                            .SendToChannel(context.Channel);
                        return;
                    }

                    int mekosGained = (int)Math.Round((((Global.random.NextDouble() + 1.25) * 0.5) / thisGuild.UserCount * 10) * thisGuild.CalculateLevel(thisGuild.Experience));

                    User user = await database.Users.FindAsync(context.Author.Id.ToDbLong());
                    user.Currency += mekosGained;

                    await Utils.Embed
                        .SetTitle("Weekly bonus")
                        .AddInlineField("Mekos", mekosGained.ToString())
                        .SendToChannel(context.Channel);

                    timer.Value = DateTime.Now;
                    await database.SaveChangesAsync();
                }
                else
                {
                    await Utils.Embed
                        .SetTitle("Weekly")
                        .SetDescription("not available yet!")
                        .SendToChannel(context.Channel);
                }
            }
        }

        private async Task DoGuildNewRival(EventContext context)
        {
            using (MikiContext db = new MikiContext())
            {
                GuildUser thisGuild = await db.GuildUsers.FindAsync(context.Guild.Id.ToDbLong());

                List<GuildUser> rivalGuilds = db.GuildUsers.Where((g) => (g.UserCount - thisGuild.UserCount) < 5 && g.RivalId == 0 && g.Id != thisGuild.Id).ToList();

                if(rivalGuilds.Count == 0)
                {
                    await Utils.Embed
                        .SetTitle("Whoopsie!")
                        .SetDescription("We couldn't matchmake you right now, try again later!")
                        .SendToChannel(context.Channel);
                    return;
                }

                int random = Global.random.Next(0, rivalGuilds.Count);

                GuildUser rivalGuild = await db.GuildUsers.FindAsync(rivalGuilds[random].Id);

                thisGuild.RivalId = rivalGuild.Id;
                rivalGuild.RivalId = thisGuild.Id;

                await db.SaveChangesAsync();

                await Utils.Embed
                    .SetTitle("Rival Set!")
                    .SetDescription($"Your new rival is **{rivalGuild.Name}**!")
                    .SendToChannel(context.Channel);
            }
        }

        private async Task DoGuildProfile(EventContext context)
        {
            Locale locale = Locale.GetEntity(context.Channel.Id);

            using (MikiContext database = new MikiContext())
            {
                GuildUser g = await database.GuildUsers.FindAsync(context.Guild.Id.ToDbLong());

                int rank = g.GetGlobalRank();
                int level = g.CalculateLevel(g.Experience);

                IDiscordEmbed embed = Utils.Embed
                    .SetTitle(g.Name)
                    .SetColor(0.1f, 0.6f, 1)
                    .AddInlineField("Level", level.ToString())
                    .AddInlineField("Experience", g.Experience + "/" + g.CalculateMaxExperience(g.Experience))
                    .AddInlineField("Rank", "#" + ((rank <= 10) ? $"**{rank}**" : rank.ToString()))
                    .AddInlineField("Users", g.UserCount.ToString())
                    .SetThumbnailUrl(context.Guild.AvatarUrl);

                if(g.RivalId != 0)
                {
                    GuildUser rival = await g.GetRival();
                    embed.AddInlineField("Rival", $"{rival.Name} [{rival.Experience}]");
                }

                await embed.SendToChannel(context.Channel);
            }
        }

        private async Task DoSetDonator(EventContext context)
        {
            using (MikiContext database = new MikiContext())
            {
                if (context.message.MentionedUserIds.Count > 0)
                {
                    Achievement a = await database.Achievements.FindAsync(context.message.MentionedUserIds.First().ToDbLong(), "donator");
                    if(a == null)
                    {
                        database.Achievements.Add(new Achievement() { Id = context.message.MentionedUserIds.First().ToDbLong(), Name = "donator", Rank = 0 });
                        await database.SaveChangesAsync();
                    }

                }
                else
                {
                    ulong x = 0;
                    ulong.TryParse(context.message.Content, out x);
                    if (x != 0)
                    {
                        database.Achievements.Add(new Achievement() { Id = x.ToDbLong(), Name = "donator", Rank = 0 });
                        await database.SaveChangesAsync();
                    }
                }
                await context.Channel.SendMessage(":ok_hand:");
            }
        }

        private async Task DoCmdTest(EventContext message)
        {
            CommandHandler c = new CommandHandlerBuilder()
                .AddPrefix(">")
                .DisposeInSeconds(20)
                .SetOwner(message.message)
                .AddCommand(
                    new RuntimeCommandEvent("yes")
                        .Default(async (e) => 
                        {
                            await e.Channel.SendMessage("you picked yes!");
                            e.commandHandler.RequestDispose();
                        }))
                .AddCommand(
                    new RuntimeCommandEvent("no")
                        .Default(async (e) => 
                        {
                            await e.Channel.SendMessage("you picked no!");
                            e.commandHandler.RequestDispose();
                        }))
                .Build();

            Bot.instance.Events.AddPrivateCommandHandler(message.message, c);
            await message.Channel.SendMessage("OK!");
        }

        private async Task DoSetMekos(EventContext e)
        {
            using (var context = new MikiContext())
            {
                User u = await context.Users.FindAsync(e.message.MentionedUserIds.First().ToDbLong());
                if (u == null)
                {
                    return;
                }
                u.Currency = int.Parse(e.arguments.Split(' ')[1]);
                await context.SaveChangesAsync();
            }
        }
    }
}