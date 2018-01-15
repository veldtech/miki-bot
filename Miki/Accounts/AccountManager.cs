using Discord;
using IA;
using IA.SDK;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.Models;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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
					Experience = MikiRandom.Next(4, 10),
					Name = e.Author.Username,
				});

				lastTimeExpGranted[e.Author.Id] = DateTime.Now;
			}

			if (DateTime.Now >= lastDbSync + new TimeSpan(0, 1, 0))
			{
				Log.Message($"Applying Experience for {experienceQueue.Count} users");
				lastDbSync = DateTime.Now;
				try
				{
					await UpdateGlobalDatabase();
					await UpdateLocalDatabase();
					await UpdateGuildDatabase();
				}
				catch(Exception ex)
				{
					Log.Error(ex.Message);
				}
				experienceQueue.Clear();
				Log.Message($"Done Applying!");
				experienceQueue.Clear();
			}
		}

		public async Task UpdateGlobalDatabase()
		{
			List<ExperienceAdded> queue = experienceQueue.ToList();
			Dictionary<long, ExperienceAdded> usersToUpdate = new Dictionary<long, ExperienceAdded>();

			List<string> userQuery = new List<string>();
			string x = "WITH new_values (id, name, experience) as (values";

			List<string> userParameters = new List<string>();

			for (int i = 0; i < queue.Count; i++)
			{
				if (!usersToUpdate.ContainsKey(queue[i].UserId))
				{
					usersToUpdate.Add(queue[i].UserId, queue[i]);
				}
				else
				{
					usersToUpdate[queue[i].UserId].Experience += queue[i].Experience;
				}
			}

			for (int i = 0; i < usersToUpdate.Values.Count; i++)
			{
				userQuery.Add($"({usersToUpdate.Values.ElementAt(i).UserId}, @p{i}, {usersToUpdate.Values.ElementAt(i).Experience})");
				userParameters.Add(usersToUpdate.Values.ElementAt(i).Name ?? "name failed to set?");
			}

			string y = $"),upsert as ( update \"dbo\".\"Users\" m set \"Total_Experience\" = \"Total_Experience\" + nv.experience FROM new_values nv WHERE m.\"Id\" = nv.id RETURNING m.*) INSERT INTO \"dbo\".\"Users\"(\"Id\", \"Name\", \"Total_Experience\") SELECT id, name, experience FROM new_values WHERE NOT EXISTS(SELECT * FROM upsert up WHERE up.\"Id\" = new_values.id);";

			string query = x + string.Join(",", userQuery) + y;

			using (var context = new MikiContext())
			{
				context.Database.Log += Console.Write;
				await context.Database.ExecuteSqlCommandAsync(query, userParameters.ToArray());
				await context.SaveChangesAsync();
			}
		}
		public async Task UpdateLocalDatabase()
		{
			List<ExperienceAdded> queue = experienceQueue.ToList();
			Dictionary<Tuple<long, long>, ExperienceAdded> usersToUpdate = new Dictionary<Tuple<long, long>, ExperienceAdded>();

			List<string> userQuery = new List<string>();
			string x = "WITH new_values (id, serverid, experience) as (values ";

			for (int i = 0; i < queue.Count; i++)
			{
				if (!usersToUpdate.ContainsKey(new Tuple<long, long>(queue[i].UserId, queue[i].GuildId)))
				{
					usersToUpdate.Add(new Tuple<long, long>(queue[i].UserId, queue[i].GuildId), queue[i]);
				}
				else
				{
					usersToUpdate[new Tuple<long, long>(queue[i].UserId, queue[i].GuildId)].Experience += queue[i].Experience;
				}
			}

			for (int i = 0; i < usersToUpdate.Values.Count; i++)
			{
				userQuery.Add($"({usersToUpdate.Values.ElementAt(i).UserId}, {usersToUpdate.Values.ElementAt(i).GuildId}, {usersToUpdate.Values.ElementAt(i).Experience})");
			}

			string y = $"),upsert as(update \"dbo\".\"LocalExperience\" m set \"Experience\" = \"Experience\" + nv.experience FROM new_values nv WHERE m.\"UserId\" = nv.id AND m.\"ServerId\" = nv.serverid RETURNING m.*) INSERT INTO \"dbo\".\"LocalExperience\"(\"UserId\", \"ServerId\", \"Experience\") SELECT id, serverid, experience FROM new_values WHERE NOT EXISTS(SELECT 1 FROM upsert up WHERE up.\"UserId\" = new_values.id AND up.\"ServerId\" = new_values.serverid);";

			string query = x + string.Join(",", userQuery) + y;

			using (var context = new MikiContext())
			{
				context.Database.Log += Console.Write;
				await context.Database.ExecuteSqlCommandAsync(query);
				await context.SaveChangesAsync();
			}
		}
		public async Task UpdateGuildDatabase()
		{
			List<ExperienceAdded> queue = experienceQueue.ToList();
			Dictionary<long, ExperienceAdded> usersToUpdate = new Dictionary<long, ExperienceAdded>();

			List<string> userQuery = new List<string>();
			string x = "WITH new_values (id, experience) as (values ";

			for (int i = 0; i < queue.Count; i++)
			{
				if (!usersToUpdate.ContainsKey(queue[i].GuildId))
				{
					usersToUpdate.Add(queue[i].GuildId, queue[i]);
				}
				else
				{
					usersToUpdate[queue[i].GuildId].Experience += queue[i].Experience;
				}
			}

			for (int i = 0; i < usersToUpdate.Values.Count; i++)
			{
				userQuery.Add($"({usersToUpdate.Values.ElementAt(i).GuildId}, {usersToUpdate.Values.ElementAt(i).Experience})");
			}

			string y = $"),upsert as(update \"dbo\".\"GuildUsers\" m set \"Experience\" = \"Experience\" + nv.experience FROM new_values nv WHERE m.\"EntityId\" = nv.id RETURNING m.*) INSERT INTO \"dbo\".\"GuildUsers\"(\"EntityId\", \"Experience\") SELECT id, experience FROM new_values WHERE NOT EXISTS(SELECT 1 FROM upsert up WHERE up.\"EntityId\" = new_values.id);";

			string query = x + string.Join(",", userQuery) + y;

			using (var context = new MikiContext())
			{
				context.Database.Log += Console.Write;
				await context.Database.ExecuteSqlCommandAsync(query);
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

	public class ExperienceAdded
	{
		public long GuildId;
		public long UserId;
		public long Experience;
		public string Name;
	}
}

/*
 * WITH new_values (id, name, experience) as (values(368588048423976960, @p0, 7),(208452115947847682, @p2, 4),(106244225145806848, @p4, 8),(328022148411031552, @p6, 9),(282286965968076821, @p8, 4),(297337210838056960, @p10, 5),(255598213241896960, @p12, 8),(234039101383376896, @p14, 4),(308318836854358016, @p16, 8),(232028266813325313, @p18, 9),(292467453882138624, @p20, 9),(368581372379660300, @p22, 9),(86974007399677952, @p24, 4),(167766906978304000, @p26, 6),(207578216603320320, @p28, 8),(161598765319454720, @p30, 6),(102528327251656704, @p32, 7),(140959755349917696, @p34, 6),(295332296054145025, @p36, 9),(361747711193645056, @p38, 6),(296978092566642688, @p40, 5),(389931377757257748, @p42, 6),(352151992816107521, @p44, 7),(382727473403789314, @p46, 4),(173226533580701697, @p48, 6),(329298403873914890, @p50, 4),(313831629174996992, @p52, 9),(202499115282726912, @p54, 5),(297111584944160789, @p56, 4),(266809555353075713, @p58, 9),(252943712554975233, @p60, 8),(280468584185069570, @p62, 4),(278409951423234048, @p64, 7),(331762153097003018, @p66, 5),(317026895994683393, @p68, 7),(211344174899920896, @p70, 6),(388593536368640012, @p72, 7),(238061573321523201, @p74, 5),(231465866884153344, @p76, 7),(215299297007108096, @p78, 5),(258777263674228746, @p80, 6),(272547845511446528, @p82, 6),(297111737117835275, @p84, 6),(221438001396449281, @p86, 4),(206586819809247232, @p88, 7),(253314763558223873, @p90, 9),(338101773481017346, @p92, 9),(384552102069796866, @p94, 7),(248328276144029696, @p96, 4),(181181399166877705, @p98, 6),(234608138161094667, @p100, 7),(317484483656024065, @p102, 6),(237906165902737408, @p104, 7),(341448653044449281, @p106, 7),(153226989254344714, @p108, 8),(244298718440849409, @p110, 8),(251143156010057729, @p112, 6),(190689984137658368, @p114, 6),(271065253956026370, @p116, 7),(211633152412614656, @p118, 9),(189740387454156800, @p120, 5),(339400896217612291, @p122, 6),(283239686888226818, @p124, 6),(284099285228716033, @p126, 6),(393911215018999817, @p128, 8),(173893011879624708, @p130, 9),(157014577815486464, @p132, 5),(320334275150872588, @p134, 4),(344348491507695617, @p136, 9),(331242192842522624, @p138, 4),(359660390902136833, @p140, 9),(152645799623262208, @p142, 8),(393557568544309248, @p144, 6),(356839279327969282, @p146, 4),(277540083404636160, @p148, 6),(88486382091108352, @p150, 6),(256395275743133696, @p152, 9),(200853674275241984, @p154, 8),(116450642683363334, @p156, 7),(187304520173355008, @p158, 7),(171336636506832896, @p160, 5),(309470256743841793, @p162, 8),(238080783204614145, @p164, 6),(319045053504815114, @p166, 7),(207703375532261376, @p168, 8),(122027801476857861, @p170, 5),(373889214690885632, @p172, 6),(384865799975993374, @p174, 5),(263868476895133709, @p176, 6),(340945105743773696, @p178, 4),(81525573242859520, @p180, 7),(179378004747747329, @p182, 8),(218567192847974400, @p184, 5),(161508993666121728, @p186, 6),(250676541279698944, @p188, 7),(178691949941751808, @p190, 4),(366542070690873344, @p192, 6),(292514346553180160, @p194, 4),(324753907546980352, @p196, 9),(159927952530866177, @p198, 4),(197509509260771338, @p200, 6),(134378786786377729, @p202, 6),(219357492633665536, @p204, 5),(291356317610934272, @p206, 9),(143750184097021953, @p208, 6),(191765595094515712, @p210, 8),(186873874951045120, @p212, 4),(226058113327955978, @p214, 4),(360733145546358784, @p216, 9),(183433214960861185, @p218, 8),(161487582516084736, @p220, 8),(177744816665395200, @p222, 6),(256429843783221248, @p224, 7),(325756176891379723, @p226, 8),(307844484618780673, @p228, 4),(323218144644694017, @p230, 6),(131739760879337474, @p232, 4),(238146187977293824, @p234, 4),(264035753028222977, @p236, 7),(330384140912558080, @p238, 9),(298699188412350474, @p240, 9),(209071533065371648, @p242, 4),(191769936060743682, @p244, 5),(336086365978755072, @p246, 4),(247515100338978826, @p248, 4),(300429763926032384, @p250, 5),(325081699740418052, @p252, 5),(342247557587795969, @p254, 7),(393957318351388684, @p256, 9),(148299077178753024, @p258, 8),(290610698625744896, @p260, 6),(253701145455755264, @p262, 8),(239708335513927682, @p264, 8),(317731405171785729, @p266, 4),(251664384298975232, @p268, 9),(261023576172396556, @p270, 9),(279581340569960448, @p272, 8),(352819240404910090, @p274, 6),(342320449327202314, @p276, 4),(227246241837219841, @p278, 8),(344166018438004736, @p280, 7),(359566564711989262, @p282, 4),(138360362402578432, @p284, 6),(352893524364230677, @p286, 8),(361996812300845057, @p288, 8),(249431672762793994, @p290, 7),(328034930250481665, @p292, 7),(281571529622421504, @p294, 9),(220408916008239104, @p296, 4),(271835121940693002, @p298, 6),(350793489925275648, @p300, 4),(300707924592951307, @p302, 4),(324575202308653057, @p304, 8),(242780584894529538, @p306, 7),(369518422993666048, @p308, 8),(310677766011879424, @p310, 9),(160580775652229121, @p312, 5),(245892126146166785, @p314, 9),(322469451310039041, @p316, 5),(284664425871310848, @p318, 5),(272925392480894976, @p320, 6),(240750835963789312, @p322, 7),(191629523081625600, @p324, 9),(121098587655372800, @p326, 4),(211907150748975105, @p328, 6),(350431372781027340, @p330, 6),(210592343370366976, @p332, 5),(248789438518263808, @p334, 9),(280984422528974848, @p336, 9),(358503959394123778, @p338, 4),(243838555997208577, @p340, 5),(238407042979725334, @p342, 4),(210552092492955658, @p344, 9),(175303942010306560, @p346, 5),(237248053428486154, @p348, 8),(322938015838109696, @p350, 9),(237310340805820416, @p352, 7),(298251946996006922, @p354, 8),(175793649676845056, @p356, 4),(276544524518686721, @p358, 8),(277495520774193153, @p360, 5),(262924735485050880, @p362, 5),(299006683986788363, @p364, 8),(360157793677737987, @p366, 4),(328973134097285120, @p368, 9),(343077307570716674, @p370, 4),(228379914938613761, @p372, 6)),upsert as ( update "dbo"."Users" m set "Total_Experience" = "Total_Experience" + nv.experience FROM new_values nv WHERE m."Id" = nv.id RETURNING m.*) INSERT INTO "dbo"."Users"("Id", "Name", "Total_Experience") SELECT id, name, experience FROM new_values WHERE NOT EXISTS(SELECT * FROM upsert up WHERE up."Id" = new_values.id);

	*/