using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using Miki.Accounts.Achievements;
using Miki.Accounts.Achievements.Objects;
using Miki.API.Imageboards;
using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Interfaces;
using Miki.API.Imageboards.Objects;
using Miki.Common.Builders;
using Miki.Configuration;
using Miki.Core.API.Reminder;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Framework.Extension;
using Miki.Framework.Languages;
using NCalc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module(Name = "Fun")]
    public class FunModule
    {
		/// <summary>
		/// IMGUR API Key (RapidAPI)
		/// </summary>
		[Configurable]
		public string ImgurKey { get; set; } = "";

		/// <summary>
		/// IMGUR Client ID (RapidAPI)
		/// </summary>
		[Configurable]
		public string ImgurClientId { get; set; } = "";

		private string[] puns =
        {
                "miki_module_fun_pun_1",
                "miki_module_fun_pun_2",
                "miki_module_fun_pun_3",
                "miki_module_fun_pun_4",
                "miki_module_fun_pun_5",
                "miki_module_fun_pun_6",
                "miki_module_fun_pun_7",
                "miki_module_fun_pun_8",
                "miki_module_fun_pun_9",
                "miki_module_fun_pun_10",
                "miki_module_fun_pun_11",
                "miki_module_fun_pun_12",
                "miki_module_fun_pun_13",
                "miki_module_fun_pun_14",
                "miki_module_fun_pun_15",
                "miki_module_fun_pun_16",
                "miki_module_fun_pun_17",
                "miki_module_fun_pun_18",
                "miki_module_fun_pun_19",
                "miki_module_fun_pun_20",
                "miki_module_fun_pun_21",
                "miki_module_fun_pun_22",
                "miki_module_fun_pun_23",
                "miki_module_fun_pun_24",
                "miki_module_fun_pun_25",
                "miki_module_fun_pun_26",
                "miki_module_fun_pun_27",
                "miki_module_fun_pun_28",
                "miki_module_fun_pun_29",
                "miki_module_fun_pun_30",
                "miki_module_fun_pun_31",
                "miki_module_fun_pun_32",
                "miki_module_fun_pun_33",
                "miki_module_fun_pun_34",
                "miki_module_fun_pun_35",
                "miki_module_fun_pun_36",
                "miki_module_fun_pun_37",
                "miki_module_fun_pun_38",
                "miki_module_fun_pun_39",
                "miki_module_fun_pun_40",
                "miki_module_fun_pun_41",
                "miki_module_fun_pun_42",
                "miki_module_fun_pun_43",
                "miki_module_fun_pun_44",
                "miki_module_fun_pun_45",
                "miki_module_fun_pun_46",
                "miki_module_fun_pun_47",
                "miki_module_fun_pun_48",
                "miki_module_fun_pun_49",
                "miki_module_fun_pun_50",
                "miki_module_fun_pun_51",
                "miki_module_fun_pun_52",
        };
        private string[] reactions =
        {
                "miki_module_fun_8ball_answer_negative_1",
                "miki_module_fun_8ball_answer_negative_2",
                "miki_module_fun_8ball_answer_negative_3",
                "miki_module_fun_8ball_answer_negative_4",
                "miki_module_fun_8ball_answer_negative_5",
                "miki_module_fun_8ball_answer_neutral_1",
                "miki_module_fun_8ball_answer_neutral_2",
                "miki_module_fun_8ball_answer_neutral_3",
                "miki_module_fun_8ball_answer_neutral_4",
                "miki_module_fun_8ball_answer_neutral_5",
                "miki_module_fun_8ball_answer_positive_1",
                "miki_module_fun_8ball_answer_positive_2",
                "miki_module_fun_8ball_answer_positive_3",
                "miki_module_fun_8ball_answer_positive_4",
                "miki_module_fun_8ball_answer_positive_5",
                "miki_module_fun_8ball_answer_positive_6",
                "miki_module_fun_8ball_answer_positive_7",
                "miki_module_fun_8ball_answer_positive_8",
                "miki_module_fun_8ball_answer_positive_9"
        };

		private API.TaskScheduler<string> reminders = new API.TaskScheduler<string>();
		private Rest.RestClient imageClient = new Rest.RestClient(Global.Config.ImageApiUrl);

        public FunModule(Module m, Bot b)
        {
            ImageboardProviderPool.AddProvider(new ImageboardProvider<E621Post>(new ImageboardConfigurations
            {
				QueryKey = new Uri("http://e621.net/post/index.json?tags="),
				ExplicitTag = "rating:e",
				QuestionableTag = "rating:q",
				SafeTag = "rating:s",
				NetUseCredentials = true,
				NetHeaders = new List<Tuple<string, string>>() {
					new Tuple<string, string>("User-Agent", "Other"),
				},
				BlacklistedTags =
				{
					"loli",
					"shota",
				}
			}));
			ImageboardProviderPool.AddProvider(new ImageboardProvider<DanbooruPost>(new ImageboardConfigurations
			{
				QueryKey = new Uri("https://danbooru.donmai.us/posts.json?tags="),
				ExplicitTag = "rating:e",
				QuestionableTag = "rating:q",
				SafeTag = "rating:s",
				NetUseCredentials = true,
				NetHeaders = {
					new Tuple<string, string>("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(Global.Config.DanbooruCredentials))}"),
				},
				BlacklistedTags =
				{
					"loli",
					"shota"
				}
			}));
			ImageboardProviderPool.AddProvider(new ImageboardProvider<GelbooruPost>(new ImageboardConfigurations
			{
				QueryKey = new Uri("http://gelbooru.com/index.php?page=dapi&s=post&q=index&json=1&tags="),
				BlacklistedTags =
				{
					"loli",
					"shota",
				}
			}));
            ImageboardProviderPool.AddProvider(new ImageboardProvider<SafebooruPost>(new ImageboardConfigurations
			{
				QueryKey = new Uri("https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&tags="),
				BlacklistedTags =
				{
					"loli",
					"shota",
				}
			}));
            ImageboardProviderPool.AddProvider(new ImageboardProvider<Rule34Post>(new ImageboardConfigurations
			{
				QueryKey = new Uri("http://rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags="),
				BlacklistedTags =
				{
					"loli",
					"shota",
				}
			}));
            ImageboardProviderPool.AddProvider(new ImageboardProvider<KonachanPost>(new ImageboardConfigurations
			{
				QueryKey = new Uri("https://konachan.com/post.json?tags="),
				BlacklistedTags =
				{
					"loli",
					"shota",
				}
			}));

			ImageboardProviderPool.AddProvider(new ImageboardProvider<YanderePost>(new ImageboardConfigurations
			{
				QueryKey = new Uri("https://yande.re/post.json?tags="),
				BlacklistedTags =
				{
					"loli",
					"shota",
				}
			}));
        }

        [Command(Name = "8ball")]
        public async Task EightBallAsync(EventContext e)
        {
			string output = e.GetResource("miki_module_fun_8ball_result", 
				e.Author.Username, e.GetResource(reactions[MikiRandom.Next(0, reactions.Length)]));
            e.Channel.QueueMessageAsync(output);
        }

		[Command(Name = "bird", Aliases = new string[] { "birb" })]	
        public async Task BirdAsync(EventContext e)
        {
            string[] bird = {
                "http://i.imgur.com/aN948tq.jpg",
                "http://i.imgur.com/cYPsbR5.jpg",
                "http://i.imgur.com/18sRay4.jpg",
                "http://i.imgur.com/8wQYgvb.jpg",
                "http://i.imgur.com/aH32RFJ.jpg",
                "http://i.imgur.com/xP9PDiZ.jpg",
                "http://i.imgur.com/pxUOTw3.jpg",
                "http://i.imgur.com/CsDvncx.jpg",
                "http://i.imgur.com/Z5Gy32e.jpg",
                "http://i.imgur.com/4lUEQJ1.jpg",
                "http://i.imgur.com/GMkfhRN.jpg",
                "http://i.imgur.com/wTo8jAZ.jpg",
                "http://i.imgur.com/ztY0Jfs.jpg",
                "http://i.imgur.com/F7xwjRj.jpg",
                "http://i.imgur.com/N5Sn8DJ.jpg",
                "http://i.imgur.com/LNL0nuX.jpg",
                "http://i.imgur.com/DT6OVOL.jpg",
                "http://i.imgur.com/9RoC12z.jpg",
                "http://i.imgur.com/eK81Cyt.jpg",
                "http://i.imgur.com/YTz2P1p.jpg",
                "http://i.imgur.com/2nukcqZ.jpg",
                "http://i.imgur.com/BxwgwHh.jpg"
            };

			Utils.Embed
				.SetTitle("🐦 Birbs!")
				.SetColor(0.8f, 0.4f, 0.4f)
				.SetImage(bird[MikiRandom.Next(0, bird.Length)])
				.ToEmbed().QueueToChannel(e.Channel);
        }

		[Command(Name = "cat")]
		public async Task CatAsync(EventContext e)
		{
			WebClient c = new WebClient();
			byte[] b = c.DownloadData("http://aws.random.cat/meow");
			string str = Encoding.Default.GetString(b);
			CatImage cat = JsonConvert.DeserializeObject<CatImage>(str);

			Utils.Embed
				.SetTitle("🐱 Kitties!")
				.SetColor(0.8f, 0.6f, 0.4f)
				.SetImage(cat.File)
				.ToEmbed()
				.QueueToChannel(e.Channel);
		}

		[Command(Name = "compliment")]
        public async Task ComplimentAsync(EventContext e)
        {
            string[] I_LIKE =
            {
                "I like ",
                "I love ",
                "I admire ",
                "I really enjoy ",
                "For some reason I like "
            };

            string[] BODY_PART =
            {
                "that silly fringe of yours",
                "the lower part of your lips",
                "the smallest toe on your left foot",
                "the smallest toe on your right foot",
                "the second eyelash from your left eye",
                "the lower part of your chin",
                "your creepy finger on your left hand",
                "your cute smile",
                "those dazzling eyes of yours",
                "your creepy finger on your right hand",
                "the special angles your elbows make",
                "the dimples on your cheeks",
                "your smooth hair"
            };

            string[] SUFFIX =
            {
                " a lot.",
                " a bit.",
                " quite a bit.",
                " a lot, is that weird?",
            };

            e.Channel.QueueMessageAsync(I_LIKE[MikiRandom.Next(0, I_LIKE.Length)] + BODY_PART[MikiRandom.Next(0, BODY_PART.Length)] + SUFFIX[MikiRandom.Next(0, SUFFIX.Length)]);
        }

        [Command(Name = "cage")]
        public async Task CageAsync(EventContext e)
        {
            e.Channel.QueueMessageAsync("http://www.placecage.com/c/" + MikiRandom.Next(100, 1500) + "/" + MikiRandom.Next(100, 1500));
        }

        [Command(Name = "dog")]
        public async Task DogAsync(EventContext e)
        {
			string url = "";

			do
			{
				url = (await new Rest.RestClient("https://random.dog/woof").GetAsync("")).Body;
			} while (string.IsNullOrEmpty(url) || url.ToLower().EndsWith("mp4"));

			Utils.Embed
				.SetTitle("🐶 Doggo!")
				.SetColor(0.8f, 0.8f, 0.8f)
				.SetImage("https://random.dog/" + url)
				.ToEmbed().QueueToChannel(e.Channel);
        }

        [Command(Name = "gif")]
        public async Task ImgurGifAsync(EventContext e)
        {
			if (string.IsNullOrEmpty(e.Arguments.ToString()))
			{
				e.Channel.QueueMessageAsync(e.GetResource(LocaleTags.ImageNotFound));
				return;
			}

			var client = new MashapeClient(ImgurClientId, ImgurKey);
            var endpoint = new GalleryEndpoint(client);
            var images = await endpoint.SearchGalleryAsync($"title:{e.Arguments.ToString()} ext:gif");
            List<IGalleryImage> actualImages = new List<IGalleryImage>();
            foreach (IGalleryItem item in images)
            {
                if (item as IGalleryImage != null)
                {
                    actualImages.Add(item as IGalleryImage);
                }
            }

            if (actualImages.Count > 0)
            {
                IGalleryImage i = actualImages[MikiRandom.Next(0, actualImages.Count)];

                e.Channel.QueueMessageAsync(i.Link);
            }
            else
            {
                e.Channel.QueueMessageAsync(e.GetResource(LocaleTags.ImageNotFound));
            }
        }

        [Command(Name = "img")]
        public async Task ImgurImageAsync(EventContext e)
        {
			if (string.IsNullOrEmpty(e.Arguments.ToString()))
			{
				e.Channel.QueueMessageAsync(e.GetResource(LocaleTags.ImageNotFound));
				return;
			}

			var client = new MashapeClient(ImgurClientId, ImgurKey);
            var endpoint = new GalleryEndpoint(client);
            var images = await endpoint.SearchGalleryAsync($"title:{e.Arguments.ToString()}");
            List<IGalleryImage> actualImages = new List<IGalleryImage>();
            foreach (IGalleryItem item in images)
            {
                if (item as IGalleryImage != null)
                {
                    actualImages.Add(item as IGalleryImage);
                }
            }

            if (actualImages.Count > 0)
            {
                IGalleryImage i = actualImages[MikiRandom.Next(0, actualImages.Count)];

                e.Channel.QueueMessageAsync(i.Link);
            }
            else
            {
                e.Channel.QueueMessageAsync(e.GetResource(LocaleTags.ImageNotFound));
            }
        }

        [Command(Name = "pick")]
        public async Task PickAsync(EventContext e)
        {
            if (string.IsNullOrWhiteSpace(e.Arguments.ToString()))
            {
                e.Channel.QueueMessageAsync(e.GetResource(LocaleTags.ErrorPickNoArgs));
                return;
            }
            string[] choices = e.Arguments.ToString().Split(',');

            e.Channel.QueueMessageAsync(e.GetResource(LocaleTags.PickMessage, new object[] { e.Author.Username, choices[MikiRandom.Next(0, choices.Length)] }));
        }

        [Command(Name = "pun")]
        public async Task PunAsync(EventContext e)
        {
            e.Channel.QueueMessageAsync(e.GetResource(puns[MikiRandom.Next(0, puns.Length)]));
        }

		[Command(Name = "roll")]
		public async Task RollAsync(EventContext e)
		{
			string rollResult;

			if (string.IsNullOrWhiteSpace(e.Arguments.ToString())) // No Arguments.
			{
				rollResult = MikiRandom.Roll(100).ToString();
			}
			else
			{
				if (int.TryParse(e.Arguments.ToString(), out int max)) // Simple number argument.
				{
					rollResult = MikiRandom.Roll(max).ToString();
				}
				else // Assume the user has entered an advanced expression.
				{
					Regex regex = new Regex(@"(?<dieCount>\d+)d(?<dieSides>\d+)");
					string fullExpression = e.Arguments.ToString();
					int expressionCount = 0;

					foreach (Match match in regex.Matches(e.Arguments.ToString()))
					{
						GroupCollection groupCollection = match.Groups;
						int dieCount = int.Parse(groupCollection["dieCount"].Value);
						int dieSides = int.Parse(groupCollection["dieSides"].Value);
						string partialExpression = "";

						for (int i = 0; i < dieCount; i++)
						{
							partialExpression += MikiRandom.Roll(dieSides).ToString();
							if (i + 1 < dieCount)
								partialExpression += " + ";
						}

						fullExpression = regex.Replace(fullExpression, $"( {partialExpression} )", 1);
						expressionCount++;
					}

					if (expressionCount > 1)
						fullExpression = $"( {fullExpression} )";

					Expression evaluation = new Expression(fullExpression);
					rollResult = evaluation.Evaluate().ToString() + $" `{fullExpression}`";
				}
			}

			if (rollResult == "1" || rollResult.StartsWith("1 "))
			{
				await AchievementManager.Instance.GetContainerById("badluck").CheckAsync(new BasePacket()
				{
					discordUser = e.Author,
					discordChannel = e.Channel
				});
			}

			rollResult = Regex.Replace(rollResult, @"(\s)\s+", "$1");
			rollResult = Regex.Replace(rollResult, @"(\S)([^\d\s])", "$1 $2");

			e.Channel.QueueMessageAsync(e.GetResource(LocaleTags.RollResult, e.Author.Username, rollResult));
		}

		[Command(Name = "roulette")]
		public async Task RouletteAsync(EventContext e)
		{
			IEnumerable<IDiscordUser> users = await e.Guild.GetUsersAsync();
			List<IDiscordUser> realUsers = users.Where(user => !user.IsBot).ToList();

			string mention = "<@" + realUsers[MikiRandom.Next(0, realUsers.Count)].Id + ">";
			string send = string.IsNullOrEmpty(e.Arguments.ToString()) ?
				e.GetResource(LocaleTags.RouletteMessageNoArg, mention) :
				e.GetResource(LocaleTags.RouletteMessage, e.Arguments.ToString(), mention);

			e.Channel.QueueMessageAsync(send);
		}

        [Command(Name = "reminder", Aliases = new[] { "remind" })]
        public async Task RemindAsync(EventContext e)
        {
			string lowercaseArguments = e.Arguments.ToString().ToLower().Split(' ')[0];

            switch(lowercaseArguments)
            { 
				case "-clear":
				{
					await CancelReminderAsync(e);
				} break;
				case "-list":
				{
					await ListRemindersAsync(e);
				} break;
				default:
				{
					if (string.IsNullOrWhiteSpace(e.Arguments.ToString()) || e.Arguments.ToString().StartsWith("-"))
					{
						await HelpReminderAsync(e);
					}
					else
					{
						await PlaceReminderAsync(e);
					}
				} break;
			}
	    }

		private async Task PlaceReminderAsync(EventContext e)
		{
			string args = e.Arguments.Join().Argument;

			int inIndex = args.ToLower().LastIndexOf(" in ");
			int everyIndex = args.ToLower().LastIndexOf(" every ");

			// TODO: still a bit hacky
			bool isIn = (inIndex > everyIndex);
			bool repeated = false;

			int splitIndex = isIn ? inIndex : everyIndex;

			if (splitIndex == -1)
			{
				e.ErrorEmbed(e.GetResource("error_argument_null", "time"))
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			if(!isIn)
			{
				repeated = true;
			}

			string reminderText = new string(args
				.Take(splitIndex)
				.ToArray()
			);

			TimeSpan timeUntilReminder = args.GetTimeFromString();

			if (timeUntilReminder > new TimeSpan(0, 10, 0))
			{
				int id = reminders.AddTask(e.Author.Id, (context) =>
				{
					Utils.Embed.SetTitle("⏰ Reminder")
						.SetDescription(new MessageBuilder()
							.AppendText(context)
							.BuildWithBlockCode())
						.ToEmbed().QueueToChannel(e.Author.GetDMChannelAsync().Result);
				}, reminderText, timeUntilReminder, repeated);

				Utils.Embed.SetTitle($"👌 {e.GetResource("term_ok")}")
					.SetDescription($"I'll remind you to **{reminderText}** {(repeated ? "every" : "in")} **{await timeUntilReminder.ToTimeStringAsync(e.Channel.Id)}**\nYour reminder code is `{id}`")
					.SetColor(255, 220, 93)
					.ToEmbed().QueueToChannel(e.Channel);
			}
			else
			{
				e.ErrorEmbed("Sorry, but I can only remind you something after 10 minutes.")
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		private async Task CancelReminderAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();
			arg = arg?.Next();

			if (arg == null)
			{
				e.ErrorEmbed(e.GetResource("error_argument_null", "id"))
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			if (Utils.IsAll(arg))
			{
				if (reminders.GetAllInstances(e.Author.Id) is List<TaskInstance<string>> instances)
				{
					instances.ForEach(i => i.Cancel());
				}

				Utils.Embed
					.SetTitle($"⏰ {e.GetResource("reminders")}")
					.SetColor(0.86f, 0.18f, 0.26f)
					.SetDescription(e.GetResource("reminder_cancelled_all"))
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}
			else if (int.TryParse(arg.Argument, out int id))
			{
				if (reminders.CancelReminder(e.Author.Id, id) is TaskInstance<string> i)
				{
					Utils.Embed
						.SetTitle($"⏰ {e.GetResource("reminders")}")
						.SetColor(0.86f, 0.18f, 0.26f)
						.SetDescription(e.GetResource("reminder_cancelled", $"`{i.Context}`"))
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}
			}
			e.ErrorEmbed(e.GetResource("error_reminder_null"))
				.ToEmbed().QueueToChannel(e.Channel);
		}

		private async Task ListRemindersAsync(EventContext e)
		{
			var instances = reminders.GetAllInstances(e.Author.Id);
			if(instances?.Count > 0)
			{
				instances = instances.OrderBy(x => x.Id).ToList();

				EmbedBuilder embed = new EmbedBuilder()
					.SetTitle($"⏰ {e.GetResource("reminders")}")
					.SetColor(0.86f, 0.18f, 0.26f);

				foreach (var x in instances)
				{
					string tx = x.Context;
					string status = "▶";

					if (x.Context.Length > 30)
					{
						tx = new string(x.Context.Take(27).ToArray()) + "...";
					}

					if(x.RepeatReminder)
					{
						status = "🔁";
					}

					embed.Description += 
						$"{status} `{x.Id.ToString().PadRight(3)} - {tx.PadRight(30)} : {await x.TimeLeft.ToTimeStringAsync(e.Channel.Id, true)}`\n";
				}
				embed.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			e.ErrorEmbed(e.GetResource("error_no_reminders"))
				.ToEmbed().QueueToChannel(e.Channel);
		}

		private async Task HelpReminderAsync(EventContext e)
		{
			string prefix = await e.EventSystem.GetCommandHandler<SimpleCommandHandler>().GetPrefixAsync(e.Guild.Id);

			new EmbedBuilder()
				.SetTitle($"⏰ {e.GetResource("reminders")}")
				.SetColor(0.86f, 0.18f, 0.26f)
				.SetDescription(e.GetResource("reminder_help_description"))
				.AddInlineField(e.GetResource("term_commands"), 
				$"`{prefix}{e.GetResource("reminder_help_add")}` - {e.GetResource("reminder_desc_add")}\n" +
				$"`{prefix}{e.GetResource("reminder_help_clear")}` - {e.GetResource("reminder_desc_clear")}\n" +
				$"`{prefix}{e.GetResource("reminder_help_list")}` - {e.GetResource("reminder_desc_list")}\n")
			.ToEmbed().QueueToChannel(e.Channel);
		}

		[Command(Name = "safe")]
        public async Task DoSafe(EventContext e)
        {
            ILinkable s = null;

			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg != null)
			{
				string useArg = arg.Argument;

				if (useArg.ToLower().StartsWith("use"))
				{
					arg = arg.Next();

					switch (useArg.Split(':')[1].ToLower())
					{
						case "safebooru":
						{
							s = await ImageboardProviderPool.GetProvider<SafebooruPost>().GetPostAsync(arg?.TakeUntilEnd().Argument, ImageboardRating.SAFE);
						}
						break;

						case "gelbooru":
						{
							s = await ImageboardProviderPool.GetProvider<GelbooruPost>().GetPostAsync(arg?.TakeUntilEnd().Argument, ImageboardRating.SAFE);
						}
						break;

						case "konachan":
						{
							s = await ImageboardProviderPool.GetProvider<KonachanPost>().GetPostAsync(arg?.TakeUntilEnd().Argument, ImageboardRating.SAFE);
						}
						break;

						case "e621":
						{
							s = await ImageboardProviderPool.GetProvider<E621Post>().GetPostAsync(arg?.TakeUntilEnd().Argument, ImageboardRating.SAFE);
						}
						break;

						default:
						{
							e.Channel.QueueMessageAsync("I do not support this image host :(");
						}
						break;
					}
				}
				else
				{
					s = await ImageboardProviderPool.GetProvider<SafebooruPost>().GetPostAsync(e.Arguments.Join()?.Argument ?? "", ImageboardRating.SAFE);
				}
			}
			else
			{
				s = await ImageboardProviderPool.GetProvider<SafebooruPost>().GetPostAsync(e.Arguments.Join()?.Argument ?? "", ImageboardRating.SAFE);
			}

            if (s == null)
            {
                e.ErrorEmbed("We couldn't find an image with these tags!").ToEmbed().QueueToChannel(e.Channel);
                return;
            }

            e.Channel.QueueMessageAsync(s.Url);
        }

		[Command(Name = "ship")]
		public async Task ShipAsync(EventContext e)
		{
			ArgObject o = e.Arguments.First().TakeUntilEnd();

			IDiscordGuildUser user = await o.GetUserAsync(e.Guild);

			// TODO: implement UserNullException
			if (user == null)
				return;

			if (!await Global.RedisClient.ExistsAsync($"user:{e.Author.Id}:avatar:synced"))
				await Utils.SyncAvatarAsync(e.Author);

			if (!await Global.RedisClient.ExistsAsync($"user:{user.Id}:avatar:synced"))
				await Utils.SyncAvatarAsync(user);

			Random r = new Random((int)((e.Author.Id + user.Id + (ulong)DateTime.Now.DayOfYear) % int.MaxValue));

			int value = r.Next(0, 100);

			Stream s = await imageClient.GetStreamAsync($"/api/ship?me={e.Author.Id}&other={user.Id}&value={value}");
			await e.Channel.SendFileAsync(s, "meme.png");
		}

        [Command( Name = "greentext", Aliases = new string[] { "green", "gt" } )]
        public async Task GreentextAsync( EventContext e )
        {
            string[] images = new string[]
            {
				"http://i.imgur.com/J2DLbV4.png",
                "http://i.imgur.com/H0kDub9.jpg",
                "http://i.imgur.com/pBOG489.jpg",
                "http://i.imgur.com/dIxeGOe.jpg",
                "http://i.imgur.com/p7lFyrY.jpg",
                "http://i.imgur.com/8qPmX5V.jpg",
                "http://i.imgur.com/u9orsoj.png",
                "http://i.imgur.com/EQGpV8A.jpg",
                "http://i.imgur.com/qGv3Xj1.jpg",
                "http://i.imgur.com/KFArF4B.png",
				"http://i.imgur.com/6Dv3W8V.png",
				"http://i.imgur.com/TJPnX57.png",
				"http://i.imgur.com/jle1rXs.png",
				"http://i.imgur.com/6V2wcjt.png",
				"http://i.imgur.com/KW5dBMg.jpg",
				"http://i.imgur.com/vdrAAuI.png",
				"http://i.imgur.com/QnRkQ7q.png",
				"http://i.imgur.com/sjNWj0r.jpg",
				"http://i.imgur.com/SXj7kg7.jpg",
				"http://i.imgur.com/eVwqceu.jpg",
				"http://i.imgur.com/JDOySvx.png",
				"http://i.imgur.com/fetJh3C.jpg",
				"http://i.imgur.com/iRKMtHT.png",
				"http://i.imgur.com/uxLqZXl.jpg",
				"http://i.imgur.com/6RDjjzP.jpg",
				"http://i.imgur.com/hNqXdxF.png",
				"http://i.imgur.com/xADVyFD.jpg",
				"http://i.imgur.com/JH8WqAg.jpg",
				"http://i.imgur.com/LvodsHR.jpg",
				"http://i.imgur.com/4y4wI21.jpg",
				"http://i.imgur.com/y6REP8l.png",
				"http://i.imgur.com/8gQdkwx.jpg",
				"http://i.imgur.com/JVBkdyo.jpg",
				"http://i.imgur.com/3VCDWyy.jpg",
				"http://i.imgur.com/5lGh8Vo.jpg",
				"http://i.imgur.com/ZwZvQYP.jpg",
				"http://i.imgur.com/USQa4GH.jpg",
				"http://i.imgur.com/FXHFLCH.jpg",
				"http://i.imgur.com/vRRK4qd.png",
				"http://i.imgur.com/0OycISQ.jpg",
				"http://i.imgur.com/0OycISQ.jpg",
				"http://i.imgur.com/g2vdQ6i.jpg",
				"http://i.imgur.com/3vDUWgr.png",
				"http://i.imgur.com/TN58jEQ.jpg",
				"http://i.imgur.com/94wckTB.png"
            };

			new EmbedBuilder()
				.SetImage(images[MikiRandom.Next(0, images.Length)])
				.ToEmbed().QueueToChannel(e.Channel);
        }
    }
}
