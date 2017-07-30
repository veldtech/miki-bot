#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Discord;
using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Builders;
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

namespace Miki.Modules
{
    [Module("Accounts")]
    public class AccountsModule
    {
        public AccountsModule(RuntimeModule module)
        {
            AccountManager.Instance.OnLocalLevelUp += async (a, g, l) =>
            {
                using (var context = new MikiContext())
                {
                    long guildId = g.Id.ToDbLong();
                    List<LevelRole> rolesObtained = context.LevelRoles.AsNoTracking().Where(p => p.GuildId == guildId && p.RequiredLevel == l).ToList();
                    IDiscordUser u = await g.Guild.GetUserAsync(a.Id.FromDbLong());
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

            module.MessageRecieved = AccountManager.Instance.CheckAsync;

            LoadAchievements();
        }

        [Command(Name = "buymarriageslot")]
        public async Task BuyMarriageSlotAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                int limit = 10;

                if (user.IsDonator(context))
                {
                    limit += 5;
                }

                IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder());

                if (user.MarriageSlots >= limit)
                {
                    embed.Description = $"For now, **{limit} slots** is the max. sorry :(";

                    if (limit == 15)
                    {
                        embed.AddField("Pro tip!", "Donators get 5 more slots!");
                    }

                    embed.Color = new IA.SDK.Color(1f, 0.6f, 0.4f);
                    await embed.SendToChannel(e.Channel);
                    return;
                }

                int costForUpgrade = (user.MarriageSlots - 4) * 2500;

                embed.Description = $"Do you want to buy a marriage slot for **{costForUpgrade}**?\n\nType `>yes` to confirm.";
                embed.Color = new IA.SDK.Color(0.4f, 0.6f, 1f);
                await embed.SendToChannel(e.Channel);

                CommandHandler c = new CommandHandlerBuilder()
                    .AddPrefix(">")
                    .DisposeInSeconds(20)
                    .SetOwner(e.message)
                    .AddCommand(
                        new RuntimeCommandEvent("yes")
                            .Default(async (cont) =>
                            {
                                await ConfirmBuyMarriageSlot(cont, costForUpgrade);
                            }))
                            .Build();

                Bot.instance.Events.AddPrivateCommandHandler(e.message, c);
            }
        }

        [Command(Name = "leaderboards")]
        public async Task LeaderboardsAsync(EventContext e)
        {
            switch (e.arguments.ToLower())
            {
                case "local":
                case "server":
                case "guild":
                    {
                        await ShowLeaderboardsAsync(e.message, LeaderboardsType.LocalExperience);
                    }
                    break;
                case "commands":
                case "cmds":
                    {
                        await ShowLeaderboardsAsync(e.message, LeaderboardsType.Commands);
                    }
                    break;
                case "currency":
                case "mekos":
                case "money":
                    {
                        await ShowLeaderboardsAsync(e.message, LeaderboardsType.Currency);
                    }
                    break;
                default:
                    {
                        await ShowLeaderboardsAsync(e.message);
                    }
                    break;
            }
        }

        [Command(Name = "profile")]
        public async Task ProfileAsync(EventContext e)
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();

            using (var context = new MikiContext())
            {
                long id = 0;
                ulong uid = 0;

                if (e.message.MentionedUserIds.Count() > 0)
                {
                    uid = e.message.MentionedUserIds.First();
                    id = uid.ToDbLong();
                }
                else
                {
                    uid = e.message.Author.Id;
                    id = uid.ToDbLong();
                }

                User account = await context.Users.FindAsync(id);
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                IDiscordUser discordUser = await e.Guild.GetUserAsync(uid);

                if (account != null)
                {
                    IDiscordEmbed embed = Utils.Embed
                        .SetDescription(account.Title)
                        .SetAuthor(locale.GetString("miki_global_profile_user_header", account.Name), "http://veld.one/assets/profile-icon.png", "https://patreon.com/mikibot")
                        .SetThumbnailUrl(discordUser.AvatarUrl);

                    long serverid = e.Guild.Id.ToDbLong();

                    LocalExperience localExp = await context.Experience.FindAsync(serverid, id);
                    int globalExp = account.Total_Experience;

                    int rank = await account.GetLocalRank(e.Guild.Id);

                    EmojiBarSet onBarSet = new EmojiBarSet("<:mbaronright:334479818924228608>", "<:mbaronmid:334479818848468992>", "<:mbaronleft:334479819003789312>");
                    EmojiBarSet offBarSet = new EmojiBarSet("<:mbaroffright:334479818714513430>", "<:mbaroffmid:334479818504536066>", "<:mbaroffleft:334479818949394442>");


                    EmojiBar expBar = new EmojiBar(account.CalculateMaxExperience(localExp.Experience), onBarSet, offBarSet, 6);

                    string infoValue = new MessageBuilder()
                        .AppendText(locale.GetString("miki_module_accounts_information_level", account.CalculateLevel(localExp.Experience), localExp.Experience, account.CalculateMaxExperience(localExp.Experience)))
                        .AppendText(await expBar.Print(localExp.Experience, e.Channel))
                        .AppendText(locale.GetString("miki_module_accounts_information_rank", rank))
                        .AppendText("Reputation: " + account.Reputation, MessageFormatting.PLAIN, false)
                        .Build();

                    embed.AddInlineField(locale.GetString("miki_generic_information"), infoValue);

                    int globalLevel = account.CalculateLevel(account.Total_Experience);
                    int globalRank = account.CalculateMaxExperience(account.Total_Experience);

                    EmojiBar globalExpBar = new EmojiBar(account.CalculateMaxExperience(account.Total_Experience), onBarSet, offBarSet, 6);

                    string globalInfoValue = new MessageBuilder()
                        .AppendText(locale.GetString("miki_module_accounts_information_level", globalLevel, account.Total_Experience, globalRank))
                        .AppendText(await globalExpBar.Print(account.Total_Experience, e.Channel))
                        .AppendText(locale.GetString("miki_module_accounts_information_rank", account.GetGlobalRank()), MessageFormatting.PLAIN, false)
                        .Build();

                    embed.AddInlineField(locale.GetString("miki_generic_global_information"), globalInfoValue);

                    embed.AddInlineField(locale.GetString("miki_generic_mekos"), account.Currency + "🔸");

                    List<Marriage> marriages = Marriage.GetMarriages(context, id);

                    marriages = marriages.OrderBy(mar => mar.TimeOfMarriage).ToList();

                    List<User> users = new List<User>();

                    int maxCount = marriages.Count;

                    for (int i = 0; i < maxCount; i++)
                    {
                        users.Add(await context.Users.FindAsync(marriages[i].GetOther(id)));
                    }


                    if (marriages.Count > 0)
                    {
                        List<string> marriageStrings = new List<string>();

                        for (int i = 0; i < maxCount; i++)
                        {
                            if (marriages[i].GetOther(id) != 0 && marriages[i].TimeOfMarriage != null)
                            {
                                marriageStrings.Add("💕 " + users[i].Name + " (_" + marriages[i].TimeOfMarriage.ToShortDateString() + "_)");
                            }
                        }

                        embed.AddInlineField(
                            locale.GetString("miki_module_accounts_profile_marriedto"),
                            string.Join("\n", marriageStrings));
                    }

                    Random r = new Random((int)id - 3);

                    embed.Color = new IA.SDK.Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());

                    List<CommandUsage> List = context.CommandUsages.Where(c => c.UserId == id).OrderByDescending(c => c.Amount).ToList();
                    string favCommand = (List.Count > 0) ? List[0].Name + " (" + List[0].Amount + ")" : "none (yet!)";

                    embed.AddInlineField(locale.GetString("miki_module_accounts_profile_favourite_command"), favCommand);

                    string achievements = AchievementManager.Instance.PrintAchievements(context, account.Id.FromDbLong());

                    embed.AddInlineField(
                        locale.GetString("miki_generic_achievements"),
                        achievements != "" ? achievements : locale.GetString("miki_placeholder_null"));

                    embed.AddInlineField(locale.GetString("miki_module_accounts_profile_url"), "http://miki.veld.one/profile/" + account.Id);

                    embed.SetFooter(locale.GetString("miki_module_accounts_profile_footer", account.DateCreated.ToShortDateString(), sw.ElapsedMilliseconds), "");

                    sw.Stop();

                    await embed.SendToChannel(e.Channel);
                }
                else
                {
                    await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_null")).SendToChannel(e.Channel);
                }
            }
        }

        [Command(Name = "declinemarriage")]
        public async Task DeclineMarriageAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id);

            using (var context = new MikiContext())
            {
                if (e.arguments == "*")
                {
                    await Marriage.DeclineAllProposalsAsync(context, e.Author.Id.ToDbLong());
                    await e.Channel.SendMessage(locale.GetString("miki_marriage_all_declined"));
                    return;
                }

                if (e.message.MentionedUserIds.Count == 0)
                {
                    await e.Channel.SendMessage(locale.GetString("miki_marriage_no_mention"));
                    return;
                }

                Marriage marriage = await Marriage.GetEntryAsync(context, e.message.MentionedUserIds.First(), e.Author.Id);

                if (marriage != null)
                {
                    await marriage.DeclineProposalAsync(context);
                    await e.Channel.SendMessage(locale.GetString("miki_marriage_declined"));
                }
                else
                {
                    await e.Channel.SendMessage(locale.GetString("miki_marriage_null"));
                    return;
                }
            }
        }

        [Command(Name = "divorce")]
        public async Task DivorceAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (e.message.MentionedUserIds.Count == 0)
            {
                using (MikiContext context = new MikiContext())
                {
                    List<User> users = context.Users.Where(p => p.Name.ToLower() == e.arguments.ToLower()).ToList();

                    if (users.Count == 0)
                    {
                        await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")).SendToChannel(e.Channel);
                    }
                    else if (users.Count == 1)
                    {
                        Marriage currentMarriage = Marriage.GetMarriage(context, e.Author.Id, users.First().Id.FromDbLong());
                        if (currentMarriage == null)
                        {
                            await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")).SendToChannel(e.Channel);
                            return;
                        }

                        if (currentMarriage.Proposing)
                        {
                            await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")).SendToChannel(e.Channel);
                            return;
                        }

                        await currentMarriage.DivorceAsync(context);

                        IDiscordEmbed embed = Utils.Embed;
                        embed.Title = locale.GetString("miki_module_accounts_divorce_header");
                        embed.Description = locale.GetString("miki_module_accounts_divorce_content", e.Author.Username, users.First().Name);
                        embed.Color = new IA.SDK.Color(0.6f, 0.4f, 0.1f);
                        await embed.SendToChannel(e.Channel);
                        return;
                    }
                    else
                    {
                        List<Marriage> allMarriages = Marriage.GetMarriages(context, e.Author.Id.ToDbLong());
                        bool done = false;

                        foreach (Marriage marriage in allMarriages)
                        {
                            foreach (User user in users)
                            {
                                if (marriage.GetOther(e.Author.Id) == user.Id.FromDbLong())
                                {
                                    await marriage.DivorceAsync(context);
                                    done = true;

                                    IDiscordEmbed embed = Utils.Embed;
                                    embed.Title = locale.GetString("miki_module_accounts_divorce_header");
                                    embed.Description = locale.GetString("miki_module_accounts_divorce_content", e.Author.Username, user.Name);
                                    embed.Color = new IA.SDK.Color(0.6f, 0.4f, 0.1f);
                                    await embed.SendToChannel(e.Channel);
                                    break;
                                }
                            }

                            if (done) break;
                        }
                    }
                }
            }
            else
            {
                if (e.Author.Id == e.message.MentionedUserIds.First())
                {
                    await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")).SendToChannel(e.Channel);
                    return;
                }

                using (MikiContext context = new MikiContext())
                {
                    Marriage currentMarriage = Marriage.GetMarriage(context, e.Author.Id, e.message.MentionedUserIds.First());

                    await currentMarriage.DivorceAsync(context);

                    string user1 = (await e.Guild.GetUserAsync(currentMarriage.GetMe(e.Author.Id))).Username;
                    string user2 = (await e.Guild.GetUserAsync(currentMarriage.GetOther(e.Author.Id))).Username;

                    IDiscordEmbed embed = Utils.Embed;
                    embed.Title = locale.GetString("miki_module_accounts_divorce_header");
                    embed.Description = locale.GetString("miki_module_accounts_divorce_content", user1, user2);
                    embed.Color = new IA.SDK.Color(0.6f, 0.4f, 0.1f);
                    await embed.SendToChannel(e.Channel);
                }
            }
        }

        [Command(Name = "showproposals")]
        public async Task ShowProposalsAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                List<Marriage> proposals = Marriage.GetProposalsReceived(context, e.Author.Id.ToDbLong());
                List<string> proposalNames = new List<string>();

                foreach (Marriage p in proposals)
                {
                    User u = await context.Users.FindAsync(p.GetOther(e.Author.Id.ToDbLong()));
                    proposalNames.Add($"{u.Name} [{u.Id}]");
                }

                IDiscordEmbed embed = Utils.Embed;
                embed.Title = e.Author.Username;
                embed.Description = "Here it shows both the people who you've proposed to and who have proposed to you.";

                string output = string.Join("\n", proposalNames);

                embed.AddField("Proposals Recieved", string.IsNullOrEmpty(output) ? "none (yet!)" : output);

                proposals = Marriage.GetProposalsSent(context, e.Author.Id.ToDbLong());
                proposalNames = new List<string>();

                foreach (Marriage p in proposals)
                {
                    User u = await context.Users.FindAsync(p.GetOther(e.Author.Id.ToDbLong()));
                    proposalNames.Add($"{u.Name} [{u.Id}]");
                }

                output = string.Join("\n", proposalNames);

                embed.AddField("Proposals Sent", string.IsNullOrEmpty(output) ? "none (yet!)" : output);

                embed.Color = new IA.SDK.Color(1, 0.5f, 0);
                embed.ThumbnailUrl = (await e.Guild.GetUserAsync(e.Author.Id)).AvatarUrl;
                await embed.SendToChannel(e.Channel);
            }
        }

        [Command(Name ="acceptmarriage")]
        public async Task AcceptMarriageAsync(EventContext e)
        {
            if (e.message.MentionedUserIds.Count == 0)
            {
                await e.Channel.SendMessage("Please mention the person you want to marry.");
                return;
            }

            using (var context = new MikiContext())
            {
                Marriage marriage = await Marriage.GetProposalReceivedAsync(context, e.message.MentionedUserIds.First(), e.Author.Id);

                if (marriage != null)
                {
                    User person1 = await context.Users.FindAsync(marriage.Id1);
                    User person2 = await context.Users.FindAsync(marriage.Id2);

                    if (person1.MarriageSlots < Marriage.GetMarriages(context, person1.Id).Count)
                    {
                        await e.Channel.SendMessage($"{person1.Name} do not have enough marriage slots, sorry :(");
                        return;
                    }

                    if (person2.MarriageSlots < Marriage.GetMarriages(context, person2.Id).Count)
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
        }

        [Command(Name = "mekos")]
        public async Task ShowMekosAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder());
                embed.Title = "🔸 Mekos";
                embed.Description = $"{user.Name} has **{user.Currency}** mekos!";
                embed.Color = new IA.SDK.Color(1f, 0.5f, 0.7f);

                await embed.SendToChannel(e.Channel);
            }
        }

        [Command(Name = "rep")]
        public async Task GiveReputationAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                User giver = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                if (e.message.MentionedUserIds.Count == 0)
                {
                    await Utils.Embed
                        .SetTitle("Reputation")
                        .SetDescription("Here are your statistics on reputation points!\n\nTo give someone reputation, do `>rep <mention>`")
                        .AddInlineField("Total Rep Received", giver.Reputation.ToString())
                        .AddInlineField("Rep points reset in:", Utils.ToTimeString(DateTime.Now.AddDays(1).Date - DateTime.Now))
                        .SendToChannel(e.Channel);
                    return;
                }

                User receiver = await context.Users.FindAsync(e.message.MentionedUserIds.First().ToDbLong());

                if (giver.LastReputationGiven.Day != DateTime.Now.Day)
                {
                    giver.ReputationPointsLeft = 3;
                    giver.LastReputationGiven = DateTime.Now;
                }

                if(giver.Id == receiver.Id)
                {
                    await Utils.Embed
                    .SetTitle("Reputation")
                    .SetDescription($"You cannot rep yourself.")
                    .SendToChannel(e.Channel);
                    return;
                }

                if (giver.ReputationPointsLeft > 0)
                {
                    giver.ReputationPointsLeft--;
                    receiver.Reputation++;

                    await Utils.Embed
                        .SetTitle("Reputation")
                        .SetDescription($"{giver.Name} has given {receiver.Name} 1 reputation! {receiver.Name} now has {receiver.Reputation} points!")
                        .AddInlineField("Points left for today", giver.ReputationPointsLeft.ToString())
                        .SendToChannel(e.Channel);
                }
                else
                {
                    await Utils.Embed
                      .SetTitle("Reputation")
                      .SetDescription($"You're out of points for today!")
                      .SendToChannel(e.Channel);
                }

                await context.SaveChangesAsync();

            }
        }

        [Command(Name = "give")]
        public async Task GiveMekosAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id);

            string[] arguments = e.arguments.Split(' ');

            if (arguments.Length < 2)
            {
                await Utils.ErrorEmbed(locale, "**Remember:** the usage is `>give <@user> <amount>`\n\nMake sure the person has a profile!").SendToChannel(e.Channel);
                return;
            }
            
            if (arguments[1].Length > 9)
            {
                await Utils.ErrorEmbed(locale, "That's too many mekos! The most I can transfer at a time is `999,999,999`").SendToChannel(e.Channel);
                return;
            }

            if (!int.TryParse(arguments[1], out int goldSent))
            {
                await Utils.ErrorEmbed(locale, "**Remember:** the usage is `>give <@user> <amount>`\n\nMake sure the person has a profile!").SendToChannel(e.Channel);
                return;
            }

            if (goldSent <= 0)
            {
                await Utils.ErrorEmbed(locale, "You have to send at least one meko.").SendToChannel(e.Channel);
                return;
            }

            if (e.message.MentionedUserIds.Count <= 0)
            {
                await Utils.ErrorEmbed(locale, "Please mention the person you want to give mekos to. Use `>help give` to find out how to use it!").SendToChannel(e.Channel);
                return;
            }

            using (var context = new MikiContext())
            {
                User sender = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                User receiver = await context.Users.FindAsync(e.message.MentionedUserIds.First().ToDbLong());

                if(receiver == null)
                {
                    await Utils.ErrorEmbed(locale, "This user doesn't have a Miki account yet.")
                        .SendToChannel(e.Channel);
                    return;
                }

                if (goldSent <= sender.Currency)
                {
                    await receiver.AddCurrencyAsync(e.Channel, sender, goldSent);
                    await sender.AddCurrencyAsync(e.Channel, sender, -goldSent);

                    string reciever = (await e.Guild.GetUserAsync(e.message.MentionedUserIds.First())).Username;

                    IDiscordEmbed em = Utils.Embed;
                    em.Title = "🔸 transaction";
                    em.Description = $"{ e.Author.Username } just gave { reciever } { goldSent } mekos!";

                    em.Color = new IA.SDK.Color(255, 140, 0);

                    await context.SaveChangesAsync();
                    await em.SendToChannel(e.Channel);
                }
                else
                {
                    IDiscordEmbed embed = Utils.Embed
                        .SetTitle("Transaction failed!")
                        .SetDescription("You do not have enough Mekos.")
                        .SetColor(255, 0, 0);

                    await embed.SendToChannel(e.Channel);
                }
            }
        }

        [Command(Name = "daily")]
        public async Task GetDailyAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                if (u == null)
                {
                    await Utils.ErrorEmbed(locale, "Your account doesn't exist in the database yet, please talk a bit more.").SendToChannel(e.Channel);
                    return;
                }

                int dailyAmount = 100;

                if (u.IsDonator(context))
                {
                    dailyAmount *= 2;
                }

                if (u.LastDailyTime.AddHours(23) >= DateTime.Now)
                {
                    await e.Channel.SendMessage($"You already claimed your daily today! Please wait another `{(u.LastDailyTime.AddHours(23) - DateTime.Now).ToTimeString()}` before using it again.");
                    return;
                }

                await u.AddCurrencyAsync(e.Channel, null, dailyAmount);
                u.LastDailyTime = DateTime.Now;

                await Utils.Embed
                    .SetTitle(locale.GetString("Daily"))
                    .SetDescription($"Received **{dailyAmount}** Mekos! You now have `{u.Currency}` Mekos")
                    .SendToChannel(e.Channel);

                await context.SaveChangesAsync();
            }
        }

        [Command(Name = "syncavatar")]
        public async Task SyncAvatarAsync(EventContext e)
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
                response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
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

            using (var context = new MikiContext())
            {
                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                if (user == null)
                {
                    return;
                }
                user.AvatarUrl = e.Author.Id.ToString();
                await context.SaveChangesAsync();
            }

            IDiscordEmbed embed = Utils.Embed;
            embed.Title = "👌 OKAY";
            embed.Description = "I've synchronized your current avatar to Miki's database!";
            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "syncname")]
        public async Task SyncNameAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                if (user == null)
                {
                    return;
                }
                user.Name = e.Author.Username;
                await context.SaveChangesAsync();
            }

            IDiscordEmbed embed = Utils.Embed;
            embed.Title = "👌 OKAY";
            embed.Description = "I've synchronized your current name to Miki's database!";
            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "setrolelevel")]
        public async Task SetRoleLevelAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                List<string> allArgs = new List<string>();
                allArgs.AddRange(e.arguments.Split(' '));
                if (allArgs.Count >= 2)
                {
                    int levelrequirement = int.Parse(allArgs[allArgs.Count - 1]);
                    allArgs.RemoveAt(allArgs.Count - 1);
                    IDiscordRole role = e.Guild.Roles.Find(r => r.Name.ToLower() == string.Join(" ", allArgs).TrimEnd(' ').TrimStart(' ').ToLower());

                    if (role == null)
                    {
                        await Utils.ErrorEmbed(locale, "Couldn't find this role. Please try again!").SendToChannel(e.Channel);
                        return;
                    }

                    LevelRole lr = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
                    if (lr == null)
                    {
                        lr = context.LevelRoles.Add(new LevelRole() { GuildId = e.Guild.Id.ToDbLong(), RoleId = role.Id.ToDbLong(), RequiredLevel = levelrequirement });

                        IDiscordEmbed embed = Utils.Embed;
                        embed.Title = "Added Role!";
                        embed.Description = $"I'll give someone the role {role.Name} when he/she reaches level {levelrequirement}!";
                        await embed.SendToChannel(e.Channel);
                    }
                    else
                    {
                        lr.RequiredLevel = levelrequirement;

                        IDiscordEmbed embed = Utils.Embed;
                        embed.Title = "Updated Role!";
                        embed.Description = $"I'll give someone the role {role.Name} when he/she reaches level {levelrequirement}!";
                        await embed.SendToChannel(e.Channel);
                    }
                    await context.SaveChangesAsync();
                }
                else
                {
                    await Utils.ErrorEmbed(locale, "Make sure to fill out both the role and the level when creating this!").SendToChannel(e.Channel);
                }
            }
        }

        [Command(Name = "marry")]
        public async Task MarryAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id);

            if (e.message.MentionedUserIds.Count == 0)
            {
                await e.Channel.SendMessage(locale.GetString("miki_module_accounts_marry_error_no_mention"));
                return;
            }


            using (MikiContext context = new MikiContext())
            {
                User mentionedPerson = await context.Users.FindAsync(e.message.MentionedUserIds.First().ToDbLong());
                User currentUser = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                IDiscordUser user = await e.Guild.GetUserAsync(e.message.MentionedUserIds.First());

                if (currentUser == null || mentionedPerson == null)
                {
                    await Utils.ErrorEmbed(locale, "miki_module_accounts_marry_error_null").SendToChannel(e.Channel);
                    return;
                }

                if (mentionedPerson.Id == currentUser.Id)
                {
                    await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_marry_error_null")).SendToChannel(e.Channel);
                    return;
                }

                if (await Marriage.ExistsAsync(context, mentionedPerson.Id, currentUser.Id))
                {
                    await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_marry_error_exists")).SendToChannel(e.Channel);
                    return;
                }

                if (await Marriage.ProposeAsync(context, currentUser.Id, mentionedPerson.Id))
                {
                    await e.Channel.SendMessage(
                        $"💍 " +
                        locale.GetString("miki_module_accounts_marry_text", $"**{e.Author.Username}**", $"**{user.Username}**") +
                        " 💍\n\n⛪ " + user.Mention + " " +
                        locale.GetString("miki_module_accounts_marry_text2") +
                        $" ⛪\n\n✅ **>acceptmarriage [@{locale.GetString("miki_terms_mention")}]**\n❌ **>declinemarriage [@{locale.GetString("miki_terms_mention")}]**");
                }
            }
        }

        private async Task ConfirmBuyMarriageSlot(EventContext cont, int costForUpgrade)
        {
            using (var context = new MikiContext())
            {
                User user = await context.Users.FindAsync(cont.Author.Id.ToDbLong());

                if (user.Currency >= costForUpgrade)
                {
                    user.MarriageSlots++;
                    user.Currency -= costForUpgrade;
                    IDiscordEmbed notEnoughMekosErrorEmbed = new RuntimeEmbed(new EmbedBuilder());
                    notEnoughMekosErrorEmbed.Color = new IA.SDK.Color(0.4f, 1f, 0.6f);
                    notEnoughMekosErrorEmbed.Description = $"You successfully purchased a new marriage slot, you now have {user.MarriageSlots} slots!";
                    await notEnoughMekosErrorEmbed.SendToChannel(cont.Channel);
                    await context.SaveChangesAsync();
                    cont.commandHandler.RequestDispose();
                }
                else
                {
                    IDiscordEmbed notEnoughMekosErrorEmbed = new RuntimeEmbed(new EmbedBuilder());
                    notEnoughMekosErrorEmbed.Color = new IA.SDK.Color(1, 0.4f, 0.6f);
                    notEnoughMekosErrorEmbed.Description = $"You do not have enough mekos! You need {costForUpgrade - user.Currency} more mekos!";
                    await notEnoughMekosErrorEmbed.SendToChannel(cont.Channel);
                    cont.commandHandler.RequestDispose();
                }
            }
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
                x.Name = "fa";
                x.Achievements = new List<CommandAchievement>()
                {
                    new CommandAchievement()
                    {
                        Name = "Lonely",
                        Icon = "😭",

                        CheckCommand = async (p) =>
                        {
                            return p.command.Name.ToLower() == "marry" && p.message.MentionedUserIds.First() == p.message.Author.Id;
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
                            if(p.command.Name.ToLower() == "createpasta")
                            {
                                return true;
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
                            return Bot.instance.Events.CommandHandler.GetUserAccessibility(p.message) < p.command.Accessibility;
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
                IDiscordEmbed embed = Utils.Embed;
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                switch (t)
                {
                    case LeaderboardsType.Commands:
                        {
                            embed.Title = locale.GetString("miki_module_accounts_leaderboards_commands_header");
                            embed.Color = new IA.SDK.Color(0.4f, 1.0f, 0.6f);
                            List<LeaderboardsItem> output = context.Database.SqlQuery<LeaderboardsItem>("select top 12 name as Name, total_commands as Value from Users order by Total_Commands desc;").ToList();
                            int i = 1;
                            foreach (LeaderboardsItem user in output)
                            {
                                embed.AddInlineField($"#{i}: {string.Join("", user.Name.Take(16))}", $"{user.Value} commands used!");
                                i++;
                            }
                            await embed.SendToChannel(e.Channel);
                        } break;
                    case LeaderboardsType.Currency:
                        {
                            embed.Title = locale.GetString("miki_module_accounts_leaderboards_mekos_header");
                            embed.Color = new IA.SDK.Color(1.0f, 0.6f, 0.4f);
                            List<LeaderboardsItem> output = context.Database.SqlQuery<LeaderboardsItem>("select top 12 name as Name, Currency as Value from Users order by Currency desc;").ToList();
                            int i = 1;
                            foreach (LeaderboardsItem user in output)
                            {
                                embed.AddInlineField($"#{i}: {string.Join("", user.Name.Take(16))}", $"{user.Value} mekos!");
                                i++;
                            }
                            await embed.SendToChannel(e.Channel);
                        } break;
                    case LeaderboardsType.LocalExperience:
                        {
                            embed.Title = locale.GetString("miki_module_accounts_leaderboards_local_header");
                            embed.Color = new IA.SDK.Color(1.0f, 0.6f, 0.4f);
                            List<LeaderboardsItem> output = context.Database.SqlQuery<LeaderboardsItem>("select top 12 name as Name, exp.Experience as Value from Users inner join LocalExperience as exp ON exp.ServerId=@p0 AND exp.UserId = Id order by exp.Experience desc;", e.Guild.Id.ToDbLong()).ToList();
                            int i = 1;
                            foreach (LeaderboardsItem user in output)
                            {
                                embed.AddInlineField($"#{i}: {string.Join("", user.Name.Take(16))}", $"{user.Value} experience!");
                                i++;
                            }
                            await embed.SendToChannel(e.Channel);
                        } break;
                    case LeaderboardsType.Experience:
                        {
                            embed.Title = locale.GetString("miki_module_accounts_leaderboards_header");
                            embed.Color = new IA.SDK.Color(1.0f, 0.6f, 0.4f);
                            List<LeaderboardsItem> output = context.Database.SqlQuery<LeaderboardsItem>("select top 12 name as Name, Total_Experience as Value from Users order by Total_Experience desc;", e.Guild.Id.ToDbLong()).ToList();
                            int i = 1;
                            foreach (LeaderboardsItem user in output)
                            {
                                embed.AddInlineField($"#{i}: {string.Join("", user.Name.Take(16))}", $"{user.Value} experience!");
                                i++;
                            }
                            await embed.SendToChannel(e.Channel);
                        } break;
                }
            }
        }
    }

    public enum LeaderboardsType
    {
        LocalExperience,
        Experience,
        Commands,
        Currency
    }
}
 
