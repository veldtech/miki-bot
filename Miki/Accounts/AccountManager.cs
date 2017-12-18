using Discord;
using IA;
using IA.SDK;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.Models;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Accounts
{
    public delegate Task LevelUpDelegate(User a, IDiscordMessageChannel g, int level);

    public class AccountManager
    {
        private static AccountManager _instance = new AccountManager(Bot.instance);
        public static AccountManager Instance => _instance;

        public event LevelUpDelegate OnLocalLevelUp;
        public event LevelUpDelegate OnGlobalLevelUp;

        public event Func<IDiscordMessage, User, User, int, Task> OnTransactionMade;
		private Queue<ExperienceAdded> experienceQueue = new Queue<ExperienceAdded>();
		private DateTime lastDbSync = DateTime.MinValue;

        private readonly Bot bot;

        private Dictionary<ulong, DateTime> lastTimeExpGranted = new Dictionary<ulong, DateTime>();

        private AccountManager(Bot bot)
        {
            this.bot = bot;

			OnGlobalLevelUp += async (a, e, l) =>
			{
				await Task.Yield();
				DogStatsd.Counter("levels.global", l);
			};
            OnLocalLevelUp += async (a, e, l) =>
            {
				DogStatsd.Counter("levels.local", l);
                long guildId = e.Guild.Id.ToDbLong();
                Locale locale = Locale.GetEntity(e.Id.ToDbLong());
                List<LevelRole> rolesObtained = new List<LevelRole>();

                int randomNumber = MikiRandom.Next(0, 10);
                int currencyAdded = (l * 10 + randomNumber);

                using (var context = new MikiContext())
                {
                    User user = await context.Users.FindAsync(a.Id);

                    if (user != null)
                    {
                        await user.AddCurrencyAsync(currencyAdded, e);
                        await context.SaveChangesAsync();
                    }
                    else
                    {
                        Log.Warning("User levelled up was null.");
                    }

                     rolesObtained = context.LevelRoles.AsNoTracking()
                        .Where(p => p.GuildId == guildId && p.RequiredLevel == l)
                        .ToList();
                }

                List<string> allRolesAdded = new List<string>();

                foreach(IDiscordRole role in rolesObtained)
                {
                    allRolesAdded.Add("Role: " + role.Name);
                }

                IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder())
                {
                    Title = locale.GetString("miki_accounts_level_up_header"),
                    Description = locale.GetString("miki_accounts_level_up_content", a.Name, l),
                    Color = new IA.SDK.Color(1, 0.7f, 0.2f)
                };

                embed.AddField(locale.GetString("miki_generic_reward"), "🔸" + currencyAdded.ToString() + "\n" + string.Join("\n", allRolesAdded));
                await Notification.SendChannel(e, embed);
            };

            Bot.instance.Client.GuildUpdated += Client_GuildUpdated;
            Bot.instance.Client.UserJoined += Client_UserJoined;
            Bot.instance.Client.UserLeft += Client_UserLeft;
        }

        public async Task CheckAsync(IDiscordMessage e)
        {
            if (e.Author.IsBot) return;

            if (!lastTimeExpGranted.ContainsKey(e.Author.Id))
            {
                lastTimeExpGranted.Add(e.Author.Id, DateTime.MinValue);
            }

			if (lastTimeExpGranted[e.Author.Id].AddMinutes(1) < DateTime.Now)
			{
				experienceQueue.Enqueue(new ExperienceAdded()
				{
					UserId = e.Author.Id.ToDbLong(),
					GuildId = e.Guild.Id.ToDbLong(),
					Amount = MikiRandom.Next(4, 10),
					Message = e,
				});

				lastTimeExpGranted[e.Author.Id] = DateTime.Now;
			}

			if (DateTime.Now >= lastDbSync + new TimeSpan(0, 1, 0))
			{
				lastDbSync = DateTime.Now;
				await UpdateDatabase();
			}
		}

		public async Task UpdateDatabase()
		{
			using (var context = new MikiContext())
			{
				List<User> user = new List<User>();
				List<LocalExperience> exp = new List<LocalExperience>();
				List<GuildUser> gu = new List<GuildUser>();

				while (experienceQueue.Count != 0)
				{
					var item = experienceQueue.Dequeue();

					var globalUser = await context.Users.FindAsync(item.UserId)
						?? await User.CreateAsync(context, item.Message);

					if (globalUser.Banned)
						continue;

					var localExperience = await context.Experience.FindAsync(item.GuildId, item.UserId)
						?? await LocalExperience.CreateAsync(context, item.GuildId, item.UserId);

					var guildUser = await context.GuildUsers.FindAsync(item.GuildId)
						?? await GuildUser.CreateAsync(context, item.Message.Guild);


					int currentLocalLevel = User.CalculateLevel(localExperience.Experience);
					int currentGlobalLevel = User.CalculateLevel(globalUser.Total_Experience);

					localExperience.Experience += item.Amount;
					globalUser.Total_Experience += item.Amount;
					guildUser.Experience += item.Amount;

					if (currentLocalLevel != User.CalculateLevel(localExperience.Experience))
					{
						await LevelUpLocalAsync(item.Message, globalUser, currentLocalLevel + 1);
					}

					if (currentGlobalLevel != User.CalculateLevel(globalUser.Total_Experience))
					{
						await LevelUpGlobalAsync(item.Message, globalUser, currentGlobalLevel + 1);
					}

					user.Add(globalUser);
					exp.Add(localExperience);
					gu.Add(guildUser);
				}

				context.Users.AddOrUpdate(user.ToArray());
				context.Experience.AddOrUpdate(exp.ToArray());
				context.GuildUsers.AddOrUpdate(gu.ToArray());

				await context.SaveChangesAsync();
			}
		}

        #region Events

        public async Task LevelUpLocalAsync(IDiscordMessage e, User a, int l)
        {
            await OnLocalLevelUp.Invoke(a, e.Channel, l);
        }

        public async Task LevelUpGlobalAsync(IDiscordMessage e, User a, int l)
        {
            await OnGlobalLevelUp.Invoke(a, e.Channel, l);
        }

        public async Task LogTransactionAsync(IDiscordMessage msg, User receiver, User fromUser, int amount)
        {
            await OnTransactionMade.Invoke(msg, receiver, fromUser, amount);
        }

        private async Task Client_GuildUpdated(Discord.WebSocket.SocketGuild arg1, Discord.WebSocket.SocketGuild arg2)
        {
            if (arg1.Name != arg2.Name)
            {
                using (MikiContext context = new MikiContext())
                {
                    GuildUser g = await context.GuildUsers.FindAsync(arg1.Id.ToDbLong());
                    g.Name = arg2.Name;
                    await context.SaveChangesAsync();
                }
            }
        }

        private async Task Client_UserLeft(Discord.WebSocket.SocketGuildUser arg)
        {
            await UpdateGuildUserCountAsync(arg.Guild.Id);
        }

        private async Task Client_UserJoined(Discord.WebSocket.SocketGuildUser arg)
        {
            await UpdateGuildUserCountAsync(arg.Guild.Id);
        }

        private async Task UpdateGuildUserCountAsync(ulong id)
        {
            using (MikiContext context = new MikiContext())
            {
                GuildUser g = await context.GuildUsers.FindAsync(id.ToDbLong());

                if (g == null)
                {
                    return;
                }

                g.UserCount = Bot.instance.Client.GetGuild(id).Users.Count;
                await context.SaveChangesAsync();
            }
        }

        #endregion Events
    }

	public struct ExperienceAdded
	{
		public long GuildId;
		public long UserId;
		public int Amount;
		public IDiscordMessage Message;
	}
}