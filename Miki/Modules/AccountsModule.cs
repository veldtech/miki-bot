#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Discord;
using IA;
using IA.Events;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.Accounts.Achievements.Objects;
using Miki.Languages;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static Miki.Notification;

namespace Miki.Modules
{
    internal class AccountsModule
    {
        public async Task LoadEvents(Bot bot)
        {
            AccountManager.Instance.OnLocalLevelUp += async (a, g, l) =>
            {
                using (var context = new MikiContext())
                {
                    long guildId = g.Id.ToDbLong();
                    List<LevelRole> rolesObtained = context.LevelRoles.AsNoTracking().Where(p => p.GuildId == guildId && p.RequiredLevel == l).ToList();
                    IDiscordUser u = await g.Guild.GetUserAsync(a.Id);
                    List<IDiscordRole> rolesGiven = new List<IDiscordRole>();

                    if (rolesObtained == null)
                    {
                        return;
                    }

                    foreach (LevelRole r in rolesObtained)
                    {
                        rolesGiven.Add(r.Role);
                    }

                    if (rolesGiven.Count > 0)
                    {
                        await u.AddRolesAsync(rolesGiven);
                    }
                }
            };

            IModule m = new Module(module =>
            {
                module.Name = "accounts";
                module.Events = new List<ICommandEvent>()
                {
                    new CommandEvent(x =>
                    {
                        x.Name = "leaderboards";
                        x.ProcessCommand = async (e, args) =>
                        {
                            switch(args.ToLower())
                            {
                                case "local":
                                case "server":
                                case "guild":
                                    {
                                        await ShowLeaderboardsAsync(e, LeaderboardsType.LocalExperience);
                                    } break;
                                case "commands":
                                case "cmds":
                                    {
                                       await ShowLeaderboardsAsync(e, LeaderboardsType.Commands);
                                    } break;
                                case "currency":
                                case "mekos":
                                case "money":
                                    {
                                        await ShowLeaderboardsAsync(e, LeaderboardsType.Currency);
                                    } break;
                                default:
                                    {
                                        await ShowLeaderboardsAsync(e);
                                    } break;
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "profile";
                        x.ProcessCommand = async (e, args) =>
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();

                            using(var context = new MikiContext())
                            {
                                long id = 0;
                                ulong uid = 0;

                                if (e.MentionedUserIds.Count() > 0)
                                {
                                    uid = e.MentionedUserIds.First();
                                    id = uid.ToDbLong();
                                }
                                else
                                {
                                    uid = e.Author.Id;
                                    id = uid.ToDbLong();
                                }

                                User account = await context.Users.FindAsync(id);
                                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                                IDiscordUser discordUser = await e.Guild.GetUserAsync(uid);

                                if (account != null)
                                {
                                    EmbedBuilder embed = new EmbedBuilder()
                                    {
                                        Description = account.Title,
                                        Author = new EmbedAuthorBuilder()
                                        {
                                            Name = locale.GetString("miki_global_profile_user_header", account.Name),
                                            Url = "https://patreon.com/mikibot",
                                            IconUrl = "http://veld.one/assets/profile-icon.png"
                                        },
                                    };

                                    embed.ThumbnailUrl = discordUser.AvatarUrl;

                                    long serverid = e.Guild.Id.ToDbLong();

                                    LocalExperience localExp = await context.Experience.FindAsync(serverid, id);
                                    int globalExp = account.Total_Experience;

                                    int rank = await account.GetLocalRank(e.Guild.Id);

                                    embed.AddField(f =>
                                    {
                                        f.Name = locale.GetString("miki_generic_information");
                                        f.IsInline = true;
                                        f.Value = "\n" + locale.GetString("miki_module_accounts_information_level", account.CalculateLevel(localExp.Experience), localExp.Experience, account.CalculateMaxExperience(localExp.Experience)) + "\n" + locale.GetString("miki_module_accounts_information_rank", rank);
                                    });

                                    embed.AddField(f =>
                                    {
                                        f.Name = locale.GetString("miki_generic_global_information");
                                        f.IsInline = true;
                                        f.Value = "\n" + locale.GetString("miki_module_accounts_information_level", account.CalculateLevel(account.Total_Experience), account.Total_Experience, account.CalculateMaxExperience(account.Total_Experience)) + "\n" + locale.GetString("miki_module_accounts_information_rank", account.GetGlobalRank());
                                    });

                                    embed.AddField(f =>
                                    {
                                        f.Name = locale.GetString("miki_generic_mekos");
                                        f.IsInline = true;
                                        f.Value = account.Currency + "🔸";
                                    });

                                    List<Marriage> marriages = Marriage.GetMarriages(context, id);
                                    List<User> users = new List<User>();

                                    for(int i = 0; i < marriages.Count; i++)
                                    {
                                        users.Add(await context.Users.FindAsync(marriages[i].GetOther(id)));
                                    }

                                    if(marriages.Count > 0)
                                    {
                                        string output = "";
                                        for (int i = 0; i < marriages.Count; i++)
                                        {
                                            if (marriages[i].GetOther(id) != 0 && marriages[i].TimeOfMarriage != null)
                                            {
                                                output += "💕 " + users[i].Name + " (_" + marriages[i].TimeOfMarriage.ToShortDateString() + "_)\n";
                                            }
                                        }
                                        output += "\n";
                                        embed.AddField(f =>
                                        {
                                            f.Name = locale.GetString("miki_module_accounts_profile_marriedto");
                                            f.Value = output;
                                            f.IsInline = true;
                                        });
                                    }

                                    Random r = new Random((int)id - 3);

                                    embed.Color = new Discord.Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());

                                    List<CommandUsage> List = context.CommandUsages.Where(c => c.UserId == id).OrderByDescending(c => c.Amount).ToList();
                                    string favCommand = (List.Count > 0) ? List[0].Name + " (" + List[0].Amount + ")" : "none (yet!)";

                                    embed.AddInlineField(locale.GetString("miki_module_accounts_profile_favourite_command"), favCommand);

                                    string achievements = AchievementManager.Instance.PrintAchievements(context, account.Id);

                                    embed.AddField(f =>
                                    {
                                        f.Name = locale.GetString("miki_generic_achievements");
                                        f.Value = achievements != "" ? achievements : locale.GetString("miki_placeholder_null");
                                        f.IsInline = true;
                                    });

                                    embed.AddField(f =>
                                    {
                                        f.Name = locale.GetString("miki_module_accounts_profile_url");
                                        f.Value = "http://miki.veld.one/profile/" + account.Id;
                                        f.IsInline = true;
                                    });

                                    embed.Footer = new EmbedFooterBuilder()
                                    {
                                        Text = locale.GetString("miki_module_accounts_profile_footer", account.DateCreated.ToShortDateString(), sw.ElapsedMilliseconds)
                                    };
                                    sw.Stop();

                                    await e.Channel.SendMessage(new RuntimeEmbed(embed));
                                }
                                else
                                {
                                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_null")));
                                }
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "divorce";
                        x.ProcessCommand = async (e, args) =>
                        {
                            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

                            if(e.MentionedUserIds.Count == 0)
                            {
                                using(MikiContext context = new MikiContext())
                                {
                                    List<User> users = context.Users.Where(p => p.Name.ToLower() == args.ToLower()).ToList();

                                    if(users.Count == 0)
                                    {
                                        await e.Channel.SendMessage(Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")));
                                    }
                                    else if(users.Count == 1)
                                    {
                                        Marriage currentMarriage = Marriage.GetMarriage(context, e.Author.Id, users.First().Id);
                                        if(currentMarriage == null)
                                        {
                                            await e.Channel.SendMessage(Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")));
                                            return;
                                        }

                                        if(currentMarriage.Proposing)
                                        {
                                            await e.Channel.SendMessage(Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")));
                                            return;
                                        }

                                        await currentMarriage.DivorceAsync(context);

                                        IDiscordEmbed embed = e.CreateEmbed();
                                        embed.Title = locale.GetString("miki_module_accounts_divorce_header");
                                        embed.Description = locale.GetString("miki_module_accounts_divorce_content", e.Author.Username, users.First().Name);
                                        embed.Color = new IA.SDK.Color(0.6f, 0.4f, 0.1f);
                                        await e.Channel.SendMessage(embed);
                                        return;
                                    }
                                    else
                                    {
                                        List<Marriage> allMarriages = Marriage.GetMarriages(context, e.Author.Id.ToDbLong());
                                        bool done = false;

                                        foreach(Marriage marriage in allMarriages)
                                        {
                                            foreach(User user in users)
                                            {
                                                if(marriage.GetOther(e.Author.Id) == user.Id)
                                                {
                                                    await marriage.DivorceAsync(context);
                                                    done = true;

                                                    IDiscordEmbed embed = e.CreateEmbed();
                                                    embed.Title = locale.GetString("miki_module_accounts_divorce_header");
                                                    embed.Description = locale.GetString("miki_module_accounts_divorce_content", e.Author.Username, user.Name);
                                                    embed.Color = new IA.SDK.Color(0.6f, 0.4f, 0.1f);
                                                    await e.Channel.SendMessage(embed);
                                                    break;
                                                }
                                            }

                                            if(done) break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if(e.Author.Id == e.MentionedUserIds.First())
                                {
                                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")));
                                    return;
                                }

                                using(MikiContext context = new MikiContext())
                                {
                                    Marriage currentMarriage = Marriage.GetMarriage(context, e.Author.Id, e.MentionedUserIds.First());

                                    await currentMarriage.DivorceAsync(context);

                                    string user1 = (await e.Guild.GetUserAsync(currentMarriage.GetMe(e.Author.Id))).Username;
                                    string user2 = (await e.Guild.GetUserAsync(currentMarriage.GetOther(e.Author.Id))).Username;

                                    IDiscordEmbed embed = e.CreateEmbed();
                                    embed.Title = locale.GetString("miki_module_accounts_divorce_header");
                                    embed.Description = locale.GetString("miki_module_accounts_divorce_content", user1, user2);
                                    embed.Color = new IA.SDK.Color(0.6f, 0.4f, 0.1f);
                                    await e.Channel.SendMessage(embed);
                                }
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "marry";
                        x.Metadata.description = "Marry your soulmate <3";
                        x.Metadata.errorMessage = "You're already proposing/married to this person.";
                        x.Metadata.usage.Add(">marry [@user]");
                        x.ProcessCommand = async (e, args) =>
                        {
                            if(e.MentionedUserIds.Count == 0)
                            {
                                await e.Channel.SendMessage("Please mention the person you're proposing to!!");
                                return;
                            }

                            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

                            using(MikiContext context = new MikiContext())
                            {
                                User mentionedPerson = await context.Users.FindAsync(e.MentionedUserIds.First().ToDbLong());
                                User currentUser = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                                IDiscordUser user = await e.Guild.GetUserAsync(e.MentionedUserIds.First());

                                if(currentUser == null || mentionedPerson == null)
                                {
                                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "You or the other person doesn't have a miki account yet. Make them talk more!! >v<"));
                                    return;
                                }

                                if(mentionedPerson.Id == currentUser.Id)
                                {
                                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "You cannot marry yourself!"));
                                    return;
                                }

                                if(await Marriage.ExistsAsync(context, mentionedPerson.Id, currentUser.Id))
                                {
                                        await e.Channel.SendMessage(Utils.ErrorEmbed(locale, x.Metadata.errorMessage));
                                        return;
                                }

                                if(await Marriage.ProposeAsync(context, currentUser.user_id, mentionedPerson.user_id))
                                {
                                    await e.Channel.SendMessage($"💍 **{ e.Author.Username }** has proposed to **{ user.Username }**! 💍\n\n⛪  {user.Mention}, Do you accept ? ⛪\n\n✅ **>acceptmarriage [@mention]**\n❌ **>declinemarriage [@mention]**");
                                }
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "acceptmarriage";
                        x.Metadata.description = "Find your true love, accept a marriage proposal!";
                        x.Metadata.usage.Add(">acceptmarriage [@user]");
                        x.ProcessCommand = async (e, args) =>
                        {
                            if(e.MentionedUserIds.Count == 0)
                            {
                                await e.Channel.SendMessage("Please mention the person you want to marry.");
                                return;
                            }

                            using(var context = new MikiContext())
                            {
                                Marriage marriage = await Marriage.GetProposalReceivedAsync(context, e.MentionedUserIds.First(), e.Author.Id);

                                if(marriage != null)
                                {
                                    User person1 = await context.Users.FindAsync(marriage.Id1);
                                    User person2 = await context.Users.FindAsync(marriage.Id2);

                                    if(person1.MarriageSlots < Marriage.GetMarriages(context, person1.user_id).Count)
                                    {
                                        await e.Channel.SendMessage($"{person1.Name} do not have enough marriage slots, sorry :(");
                                        return;
                                    }

                                    if(person2.MarriageSlots < Marriage.GetMarriages(context, person2.user_id).Count)
                                    {
                                        await e.Channel.SendMessage($"{person2.Name} does not have enough marriage slots, sorry :(");
                                        return;
                                    }

                                    marriage.AcceptProposal(context);

                                    Log.Message(marriage.Proposing.ToString());

                                    await context.SaveChangesAsync();

                                    await e.Channel.SendMessage($"❤️ Congratulations { person1.Name } and { person2.Name } ❤️");
                                }
                                else
                                {
                                    await e.Channel.SendMessage("This user hasn't proposed to you!");
                                    return;
                                }
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "declinemarriage";
                        x.Metadata.description = "only when you really don't want to get married.";
                        x.Metadata.usage.Add(">declinemarriage [@user]");
                        x.ProcessCommand = async (e, args) =>
                        {
                            using (var context = new MikiContext())
                            {
                                if(args == "*")
                                {
                                    await Marriage.DeclineAllProposalsAsync(context, e.Author.Id.ToDbLong());
                                    await e.Channel.SendMessage("All proposals declined.");
                                    return;
                                }

                                if(e.MentionedUserIds.Count == 0)
                                {
                                    await e.Channel.SendMessage("Please mention the person you want to decline.");
                                    return;
                                }

                                Marriage marriage = await Marriage.GetEntryAsync(context, e.MentionedUserIds.First(), e.Author.Id);

                                if(marriage != null)
                                {
                                    await marriage.DeclineProposalAsync(context);
                                    await e.Channel.SendMessage("The proposal has been declined.");
                                }
                                else
                                {
                                    await e.Channel.SendMessage("This user hasn't proposed to you!");
                                    return;
                                }
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "showproposals";
                        x.Metadata.errorMessage = "none (yet!)";
                        x.Metadata.description = "Show who tried to propose you.";
                        x.Metadata.usage.Add(">showproposals");
                        x.ProcessCommand = async(e, args) =>
                        {
                            using(var context = new MikiContext())
                            {
                                List<Marriage> proposals = Marriage.GetProposalsReceived(context, e.Author.Id.ToDbLong());
                                List<string> proposalNames = new List<string>();

                                foreach(Marriage p in proposals)
                                {
                                    User u = await context.Users.FindAsync(p.GetOther(e.Author.Id.ToDbLong()));
                                    proposalNames.Add($"{u.Name} [{u.Id}]");
                                }

                                EmbedBuilder embed = new EmbedBuilder();
                                embed.Title = e.Author.Username;
                                embed.Description = "Here it shows both the people who you've proposed to, and who have proposed to you.";

                                string output = string.Join("\n", proposalNames);

                                embed.AddField(f =>
                                {
                                    f.Name = "Proposals Recieved";
                                    f.Value = string.IsNullOrEmpty(output)?"none (yet!)":output;
                                });

                                proposals = Marriage.GetProposalsSent(context, e.Author.Id.ToDbLong());
                                proposalNames = new List<string>();

                                foreach(Marriage p in proposals)
                                {
                                    User u = await context.Users.FindAsync(p.GetOther(e.Author.Id.ToDbLong()));
                                    proposalNames.Add($"{u.Name} [{u.Id}]");
                                }

                                output = string.Join("\n", proposalNames);

                                embed.AddField(f =>
                                {
                                    f.Name = "Proposals Sent";
                                    f.Value = string.IsNullOrEmpty(output)?"none (yet!)":output;
                                });

                                embed.Color = new Discord.Color(1, 0.5f, 0);
                                embed.ThumbnailUrl = (await e.Guild.GetUserAsync(e.Author.Id)).AvatarUrl;
                                await e.Channel.SendMessage(new RuntimeEmbed(embed));
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "mekos";
                        x.ProcessCommand = async (e, args) =>
                        {
                            using(var context = new MikiContext())
                            {
                                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                                IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder());
                                embed.Title = "🔸 Mekos";
                                embed.Description = $"{user.Name} has **{user.Currency}** mekos!";
                                embed.Color = new IA.SDK.Color(1f, 0.5f, 0.7f);

                                await e.Channel.SendMessage(embed);
                            }   
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "daily";
                        x.Metadata.description = "Get daily Mekos!";
                        x.Metadata.usage.Add(">daily");
                        x.ProcessCommand = async (e, args) =>
                        {
                            using(var context = new MikiContext())
                            {
                                Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

                                User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                                if(u == null)
                                {
                                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "Your account doesn't exist in the database yet, please talk a bit more."));
                                    return;
                                }

                                int dailyAmount = 100;
                                 
                                if(u.IsDonator(context))
                                {
                                    dailyAmount *= 2;
                                }
                                
                                if(u.LastDailyTime.AddHours(23) >= DateTime.Now)
                                {
                                    await e.Channel.SendMessage($"You already claimed your daily today! Please wait another `{(u.LastDailyTime.AddHours(23) - DateTime.Now).ToTimeString()}` before using it again.");
                                    return;
                                }

                                u.Currency += dailyAmount;
                                u.LastDailyTime = DateTime.Now;

                                await e.Channel.SendMessage($"Received **{dailyAmount}** Mekos! You now have `{u.Currency}` Mekos");
                                await context.SaveChangesAsync();
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "give";
                        x.Metadata.description = "Give love to your friends";
                        x.Metadata.usage.Add(">give **[@user]** 1000");
                        x.Metadata.errorMessage = "**Remember:** the usage is `>give <@user> <amount>`\n\nMake sure the person has profile!";
                        x.ProcessCommand = async (e, args) =>
                        {
                            Locale locale = Locale.GetEntity(e.Guild.Id);

                            int goldSent = 0;
                            string[] arguments = args.Split(' ');

                            if(arguments.Length < 2)
                            {
                                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "**Remember:** the usage is `>give <@user> <amount>`\n\nMake sure the person has profile!"));
                                return;
                            }

                            if(!int.TryParse(arguments[1], out goldSent))
                            {
                                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "**Remember:** the usage is `>give <@user> <amount>`\n\nMake sure the person has profile!"));
                                return;
                            }

                            if(goldSent <= 0)
                            {
                                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "Please mention the person you want to give mekos to. use `>help give` to find out how to use it!"));
                                return;
                            }

                            if(e.MentionedUserIds.Count <= 0)
                            {
                                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "Please mention the person you want to give mekos to. use `>help give` to find out how to use it!"));
                                return;
                            }

                            using(var context = new MikiContext())
                            {
                                User sender = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                                User receiver = await context.Users.FindAsync(e.MentionedUserIds.First().ToDbLong());

                                if(goldSent <= sender.Currency)
                                {
                                    receiver.AddCurrency(context, sender, goldSent);
                                    sender.AddCurrency(context, sender, -goldSent);

                                    string reciever = (await e.Guild.GetUserAsync(e.MentionedUserIds.First())).Username;

                                    EmbedBuilder em = new EmbedBuilder();
                                    em.Title = "🔸 transaction";
                                    em.Description = $"{ e.Author.Username } just gave { reciever } { goldSent } mekos!";

                                    em.Color = new Discord.Color(255,140,0);

                                    await context.SaveChangesAsync();
                                    await e.Channel.SendMessage(new RuntimeEmbed(em));
                                }
                                else
                                {
                                    EmbedBuilder embed = new EmbedBuilder();
                                    embed.Title = "Transaction failed!";
                                    embed.Description = "You do not have enough Mekos.";
                                    embed.Color = new Discord.Color(255, 0, 0);
                                    await e.Channel.SendMessage(new RuntimeEmbed(embed));
                                }
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "buymarriageslot";
                        x.Accessibility = EventAccessibility.DEVELOPERONLY;
                        x.Metadata = new EventMetadata(
                            "Buy more marriage slots here, the price increases with 2500 for every slot, and caps at 10 slots.",
                            "Purchase failed",
                            ">buymarriageslot");
                        x.ProcessCommand = async (e, args) =>
                        {
                            await e.Channel.SendMessage("This command is disabled because we got IP banned for it ;w;''\n\ncheck back soon!");
                            return; 

                            using(var context = new MikiContext())
                            {
                                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                                int limit = 10;

                                if(user.IsDonator(context))
                                {
                                    limit += 5;
                                }

                                IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder());

                                if(user.MarriageSlots >= limit)
                                {
                                    embed.Description = $"For now, **{limit} slots** is the max. sorry :(";

                                    if(limit == 15)
                                    {
                                        embed.AddField(f => { f.Name="Pro tip!"; f.Value="Donators get 5 more slots!"; });
                                    }

                                    embed.Color = new IA.SDK.Color(1f, 0.6f, 0.4f);
                                    await e.Channel.SendMessage(embed);
                                    return;
                                }

                                int costForUpgrade = (user.MarriageSlots - 4) * 2500;

                                embed.Description = $"Do you want to buy a marriage slot for **{costForUpgrade}**?\n\nPress the checkmark to confirm.";
                                embed.Color = new IA.SDK.Color(0.4f, 0.6f, 1f);

                                await e.Channel.SendOption(embed, e.Author,
                                    new Option("☑", async () => {
                                        if(user.Currency >= costForUpgrade)
                                        {
                                            user.MarriageSlots++;
                                            user.Currency-=costForUpgrade;
                                            IDiscordEmbed notEnoughMekosErrorEmbed = new RuntimeEmbed(new EmbedBuilder());
                                            notEnoughMekosErrorEmbed.Color = new IA.SDK.Color(0.4f, 1f, 0.6f);
                                            notEnoughMekosErrorEmbed.Description = $"You successfully purchased a new marriageslot, you now have {user.MarriageSlots} slots!";
                                            await e.Channel.SendMessage(notEnoughMekosErrorEmbed);
                                            await context.SaveChangesAsync();
                                        }
                                        else
                                        {
                                            IDiscordEmbed notEnoughMekosErrorEmbed = new RuntimeEmbed(new EmbedBuilder());
                                            notEnoughMekosErrorEmbed.Color = new IA.SDK.Color(1, 0.4f, 0.6f);
                                            notEnoughMekosErrorEmbed.Description = "You do not have enough mekos!";
                                            await e.Channel.SendMessage(notEnoughMekosErrorEmbed);
                                        }
                                    }));
                            }
                        };
                    }),
                    new CommandEvent(cmd =>
                    {
                        cmd.Name = "syncavatar";
                        cmd.Metadata = new EventMetadata("Change your profile's avatar to your discord avatar", "couldn't set this image as your avatar", ">changeavatar");
                        cmd.ProcessCommand = async (e, args) =>
                        {
                            string localFilename = @"c:\inetpub\miki.veld.one\assets\img\user\" + e.Author.Id + ".png";

                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(e.Author.GetAvatarUrl());
                            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                                // Check that the remote file was found. The ContentType
                                // check is performed since a request for a non-existent
                                // image file might be redirected to a 404-page, which would
                                // yield the StatusCode "OK", even though the image was not
                                // found.
                                if ((response.StatusCode == HttpStatusCode.OK ||
                                    response.StatusCode == HttpStatusCode.Moved ||
                                    response.StatusCode == HttpStatusCode.Redirect) &&
                                    response.ContentType.StartsWith("image",StringComparison.OrdinalIgnoreCase))
                                {

                                    // if the remote file was found, download oit
                                    using (Stream inputStream = response.GetResponseStream())
                                    using (Stream outputStream = File.OpenWrite(localFilename))
                                    {
                                        byte[] buffer = new byte[4096];
                                        int bytesRead;
                                        do
                                        {
                                            bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                                            outputStream.Write(buffer, 0, bytesRead);
                                        } while (bytesRead != 0);
                                    }
                            }

                            using(var context = new MikiContext())
                            {
                                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                                if(user == null)
                                {
                                    return;
                                }
                                user.AvatarUrl = e.Author.Id.ToString();
                                await context.SaveChangesAsync();
                            }

                            IDiscordEmbed embed = e.CreateEmbed();
                            embed.Title = "👌 OKAY";
                            embed.Description = "I've synchronized your current avatar to Miki's database!";
                            await e.Channel.SendMessage(embed);
                        };
                    }),
                    new CommandEvent(cmd =>
                    {
                        cmd.Name = "syncname";
                        cmd.ProcessCommand = async (e, args) =>
                        {
                            using(var context = new MikiContext())
                            {
                                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                                if(user == null)
                                {
                                    return;
                                }
                                user.Name = e.Author.Username;
                                await context.SaveChangesAsync();
                            }

                            IDiscordEmbed embed = e.CreateEmbed();
                            embed.Title = "👌 OKAY";
                            embed.Description = "I've synchronized your current name to Miki's database!";
                            await e.Channel.SendMessage(embed);
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "setrolelevel";
                        x.Accessibility = EventAccessibility.ADMINONLY;
                        x.ProcessCommand = async (e, args) =>
                        {
                            using(var context = new MikiContext())
                            {
                                Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

                                List<string> allArgs = new List<string>();
                                allArgs.AddRange(args.Split(' '));
                                if(allArgs.Count >= 2)
                                {
                                    int levelrequirement = int.Parse(allArgs[allArgs.Count - 1]);
                                    allArgs.RemoveAt(allArgs.Count - 1);
                                    IDiscordRole role = e.Guild.Roles.Find(r => r.Name.ToLower() == string.Join(" ", allArgs).TrimEnd(' ').TrimStart(' ').ToLower());

                                    if(role == null)
                                    {
                                        await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "Couldn't find this role, please try again!"));
                                        return;
                                    }

                                    LevelRole lr = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
                                    if(lr == null)
                                    {
                                        lr = context.LevelRoles.Add(new LevelRole(){ GuildId = e.Guild.Id.ToDbLong(), RoleId = role.Id.ToDbLong(), RequiredLevel = levelrequirement });

                                        IDiscordEmbed embed = e.CreateEmbed();
                                        embed.Title = "Added Role!";
                                        embed.Description = $"I'll give someone the role {role.Name} when he/she reaches level {levelrequirement}!";
                                        await e.Channel.SendMessage(embed);
                                    }
                                    else
                                    {
                                        lr.RequiredLevel = levelrequirement;

                                        IDiscordEmbed embed = e.CreateEmbed();
                                        embed.Title = "Updated Role!";
                                        embed.Description = $"I'll give someone the role {role.Name} when he/she reaches level {levelrequirement}!";
                                        await e.Channel.SendMessage(embed);
                                    }
                                    await context.SaveChangesAsync();
                                }
                                else
                                {
                                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "Make sure to fill out both the role and the level when creating a this!"));
                                }
                            }
                        };
                    })
                };

                module.MessageRecieved = async (msg) => await AccountManager.Instance.CheckAsync(msg);
            });

            await new RuntimeModule(m).InstallAsync(bot);

            LoadAchievements();
        }

        public void LoadAchievements()
        {
            #region Achievement Achievements            
            AchievementManager.Instance.OnAchievementUnlocked += new AchievementDataContainer<AchievementAchievement>(x =>
            {
                x.Name = "achievements";
                x.Achievements = new List<AchievementAchievement>()
                {
                    new AchievementAchievement()
                    {
                        Name = "Underachiever",
                        Icon = "✏️",
                        CheckAchievement = async (p) =>
                        {
                            return p.count > 3;
                        },
                    },
                    new AchievementAchievement()
                    {
                        Name = "Achiever",
                        Icon = "🖊️",
                        CheckAchievement = async (p) =>
                        {
                            return p.count > 5;
                        },
                    },
                    new AchievementAchievement()
                    {
                        Name = "Overachiever",
                        Icon = "🖋️",
                        CheckAchievement = async (p) =>
                        {
                            return p.count > 12;
                        },
                    }
                };
            }).CheckAsync;
            #endregion

            #region Command Achievements
            AchievementManager.Instance.OnCommandUsed += new AchievementDataContainer<CommandAchievement>(x =>
            {
                x.Name = "info";
                x.Achievements = new List<CommandAchievement>()
                {
                    new CommandAchievement()
                    {
                        Name = "Informed",
                        Icon = "📚",

                        CheckCommand = async (p) =>
                        {
                            return p.command.Name.ToLower() == "info";
                        }
                    }
                };
            }).CheckAsync;
            AchievementManager.Instance.OnCommandUsed += new AchievementDataContainer<CommandAchievement>(x =>
            {
                x.Name = "creator";
                x.Achievements = new List<CommandAchievement>()
                {
                    new CommandAchievement()
                    {
                        Name = "Chef",
                        Icon = "📝",
                        CheckCommand = async (p) =>
                        {
                            if(p.command.Name.ToLower() == "pasta")
                            {
                                if(p.message.Content.Split(' ').Length < 2) return false;

                                switch(p.message.Content.Split(' ')[1])
                                {
                                    case "new":
                                    case "+":
                                    return true;
                                }
                            }
                            return false;
                        },
                    }
                };
            }).CheckAsync;
            AchievementManager.Instance.OnCommandUsed += new AchievementDataContainer<CommandAchievement>(x =>
            {
                x.Name = "noperms";
                x.Achievements = new List<CommandAchievement>()
                {
                    new CommandAchievement()
                    {
                        Name = "NO! Don't touch that!",
                        Icon = "😱",
                        CheckCommand = async (p) =>
                        {
                            return Bot.instance.Events.GetUserAccessibility(p.message) < p.command.Accessibility;
                        },
                    }
                };
            }).CheckAsync;
            #endregion

            #region Level Achievements
            AchievementManager.Instance.OnLevelGained += new AchievementDataContainer<LevelAchievement>(x =>
            {
                x.Name = "levelachievements";
                x.Achievements = new List<LevelAchievement>()
                {
                    new LevelAchievement()
                    {
                        Name = "Novice",
                        Icon = "🎟",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 3;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Intermediate",
                        Icon = "🎫",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 5;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Experienced",
                        Icon = "🏵",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 10;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Expert",
                        Icon = "🎗",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 20;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Sage",
                        Icon = "🎖",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 30;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Master",
                        Icon = "🏅",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 50;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Legend",
                        Icon = "💮",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 75;
                        }
                    }
                };
            }).CheckAsync;
            #endregion

            #region Message Achievements
            AchievementManager.Instance.OnMessageReceived += new AchievementDataContainer<MessageAchievement>(x =>
            {
                x.Name = "frog";
                x.Achievements = new List<MessageAchievement>()
                {
                    new MessageAchievement()
                    {
                        Name = "Oh shit! Waddup",
                        Icon = "🐸",
                        CheckMessage = async (p) =>
                        {
                            return p.message.Content.Contains("dat boi");
                        }
                    }
                };
            }).CheckAsync;
            AchievementManager.Instance.OnMessageReceived += new AchievementDataContainer<MessageAchievement>(x =>
            {
                x.Name = "lenny";
                x.Achievements = new List<MessageAchievement>()
                {
                    new MessageAchievement()
                    {
                        Name = "Lenny",
                        Icon = "😏",
                        CheckMessage = async (p) =>
                        {
                            return p.message.Content.Contains("( ͡° ͜ʖ ͡°)");
                        }
                    }
                };
            }).CheckAsync;
            AchievementManager.Instance.OnMessageReceived += new AchievementDataContainer<MessageAchievement>(x =>
            {
                x.Name = "poi";
                x.Achievements = new List<MessageAchievement>()
                {
                    new MessageAchievement()
                    {
                        Name = "Shipgirl",
                        Icon = "⛵",
                        CheckMessage = async (p) =>
                        {
                            return p.message.Content.Split(' ').Contains("poi");
                        },
                    }
                };
            }).CheckAsync;
            AchievementManager.Instance.OnMessageReceived += new AchievementDataContainer<MessageAchievement>(x =>
            {
                x.Name = "goodluck";
                x.Achievements = new List<MessageAchievement>()
                {
                    new MessageAchievement()
                    {
                        Name = "Lucky",
                        Icon = "🍀",
                        CheckMessage = async (p) =>
                        {
                            return (Global.random.Next(0, 10000000) == 5033943);
                        },
                    }
                };
            }).CheckAsync;
            #endregion
  
            #region Misc Achievements
            new AchievementDataContainer<BaseAchievement>(x =>
            {
                x.Name = "badluck";
                x.Achievements = new List<BaseAchievement>()
                {
                    new BaseAchievement()
                    {
                        Name = "Unlucky",
                        Icon = "🎲"
                    }
                };
            });
            #endregion

            #region User Update Achievements
            AchievementManager.Instance.OnUserUpdate += new AchievementDataContainer<UserUpdateAchievement>(x =>
            {
                x.Name = "contributor";
                x.Achievements = new List<UserUpdateAchievement>()
                {
                    new UserUpdateAchievement()
                    {
                        Name = "Contributor",
                        Icon = "⭐",

                        CheckUserUpdate = async (p) =>
                        {
                            if (p.userNew.Guild.Id == 160067691783127041)
                            {
                                IDiscordRole role = p.userNew.Guild.Roles.Find(r => { return r.Name == "Contributors"; });

                                if (p.userNew.RoleIds.Contains(role.Id))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                };
            }).CheckAsync;
            AchievementManager.Instance.OnUserUpdate += new AchievementDataContainer<UserUpdateAchievement>(x =>
            {
                x.Name = "developer";
                x.Achievements = new List<UserUpdateAchievement>()
                {
                    new UserUpdateAchievement()
                    {
                        Name = "Developer",
                        Icon = "🌟",
                        CheckUserUpdate = async (p) =>
                        {
                            if (p.userNew.Guild.Id == 160067691783127041)
                            {
                                IDiscordRole role = p.userNew.Guild.Roles.Find(r => { return r.Name == "Developer"; });

                                if (p.userNew.RoleIds.Contains(role.Id))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                };
            }).CheckAsync;
            AchievementManager.Instance.OnUserUpdate += new AchievementDataContainer<UserUpdateAchievement>(x =>
            {
                x.Name = "glitch";
                x.Achievements = new List<UserUpdateAchievement>()
                {
                    new UserUpdateAchievement()
                    {
                        Name = "Glitch",
                        Icon = "👾",
                        CheckUserUpdate = async (p) =>
                        {
                            if (p.userNew.Guild.Id == 160067691783127041)
                            {
                                IDiscordRole role = p.userNew.Guild.Roles.Find(r => { return r.Name == "Succesfully broke Miki"; });

                                if (p.userNew.RoleIds.Contains(role.Id))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                };
            }).CheckAsync;
            AchievementManager.Instance.OnUserUpdate += new AchievementDataContainer<UserUpdateAchievement>(x =>
            {
                x.Name = "donator";
                x.Achievements = new List<UserUpdateAchievement>()
                {
                    new UserUpdateAchievement()
                    {
                        Name = "Donator",
                        Icon = "💖",
                        CheckUserUpdate = async (p) =>
                        {
                            if (p.userNew.Guild.Id == 160067691783127041)
                            {
                                IDiscordRole role = p.userNew.Guild.Roles.Find(r => { return r.Name == "Donators"; });

                                if (p.userNew.RoleIds.Contains(role.Id))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                };
            }).CheckAsync;
            #endregion

            #region Transaction Achievements
            AchievementManager.Instance.OnTransaction += new AchievementDataContainer<TransactionAchievement>(x =>
            {
                x.Name = "meko";
                x.Achievements = new List<TransactionAchievement>()
                {
                    new TransactionAchievement()
                    {
                        Name = "Loaded",
                        Icon = "💵",
                        CheckTransaction = async (p) =>
                        {
                            return p.receiver.Currency > 10000;
                        }
                    },
                    new TransactionAchievement()
                    {
                        Name = "Rich",
                        Icon = "💸",
                        CheckTransaction = async (p) =>
                        {
                            return p.receiver.Currency > 50000;
                        }
                    },
                    new TransactionAchievement()
                    {
                        Name = "Minted",
                        Icon = "💲",
                        CheckTransaction = async (p) =>
                        {
                            return p.receiver.Currency > 125000;
                        }
                    },
                    new TransactionAchievement()
                    {
                        Name = "Millionaire",
                        Icon = "🤑",
                        CheckTransaction = async (p) =>
                        {
                            return p.receiver.Currency > 1000000;
                        }
                    }
                };
            }).CheckAsync;
            #endregion

        }

        public async Task ShowLeaderboardsAsync(IDiscordMessage e, LeaderboardsType t = LeaderboardsType.Experience)
        {
            using (var context = new MikiContext())
            {
                EmbedBuilder embed = new EmbedBuilder();
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                switch (t)
                {
                    case LeaderboardsType.Commands:
                        {
                            embed.Title = locale.GetString("miki_module_accounts_leaderboards_commands_header");
                            embed.Color = new Discord.Color(0.4f, 1.0f, 0.6f);
                            List<LeaderboardsItem> output = context.Database.SqlQuery<LeaderboardsItem>("select top 12 name as Name, total_commands as Value from Users order by Total_Commands desc;").ToList();
                            int i = 1;
                            foreach (LeaderboardsItem user in output)
                            {
                                embed.AddInlineField($"#{i}: {string.Join("", user.Name.Take(16))}", $"{user.Value} commands used!");
                                i++;
                            }
                            await e.Channel.SendMessage(new RuntimeEmbed(embed));
                        } break;
                    case LeaderboardsType.Currency:
                        {
                            embed.Title = locale.GetString("miki_module_accounts_leaderboards_mekos_header");
                            embed.Color = new Discord.Color(1.0f, 0.6f, 0.4f);
                            List<LeaderboardsItem> output = context.Database.SqlQuery<LeaderboardsItem>("select top 12 name as Name, Currency as Value from Users order by Currency desc;").ToList();
                            int i = 1;
                            foreach (LeaderboardsItem user in output)
                            {
                                embed.AddInlineField($"#{i}: {string.Join("", user.Name.Take(16))}", $"{user.Value} mekos!");
                                i++;
                            }
                            await e.Channel.SendMessage(new RuntimeEmbed(embed));
                        } break;
                    case LeaderboardsType.LocalExperience:
                        {
                            embed.Title = locale.GetString("miki_module_accounts_leaderboards_local_header");
                            embed.Color = new Discord.Color(1.0f, 0.6f, 0.4f);
                            List<LeaderboardsItem> output = context.Database.SqlQuery<LeaderboardsItem>("select top 12 name as Name, exp.Experience as Value from Users inner join LocalExperience as exp ON exp.ServerId=@p0 AND exp.UserId = Id order by exp.Experience desc;", e.Guild.Id.ToDbLong()).ToList();
                            int i = 1;
                            foreach (LeaderboardsItem user in output)
                            {
                                embed.AddInlineField($"#{i}: {string.Join("", user.Name.Take(16))}", $"{user.Value} experience!");
                                i++;
                            }
                            await e.Channel.SendMessage(new RuntimeEmbed(embed));
                        } break;
                    case LeaderboardsType.Experience:
                        {
                            embed.Title = locale.GetString("miki_module_accounts_leaderboards_header");
                            embed.Color = new Discord.Color(1.0f, 0.6f, 0.4f);
                            List<LeaderboardsItem> output = context.Database.SqlQuery<LeaderboardsItem>("select top 12 name as Name, Total_Experience as Value from Users order by Total_Experience desc;", e.Guild.Id.ToDbLong()).ToList();
                            int i = 1;
                            foreach (LeaderboardsItem user in output)
                            {
                                embed.AddInlineField($"#{i}: {string.Join("", user.Name.Take(16))}", $"{user.Value} experience!");
                                i++;
                            }
                            await e.Channel.SendMessage(new RuntimeEmbed(embed));
                        } break;
                }
            }
        }
    }

    internal enum LeaderboardsType
    {
        LocalExperience,
        Experience,
        Commands,
        Currency
    }
}