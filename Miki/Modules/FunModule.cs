using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using Miki.Accounts.Achievements;
using Miki.Accounts.Achievements.Objects;
using Miki.API.Imageboards;
using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Interfaces;
using Miki.API.Imageboards.Objects;
using Miki.Bot.Models;
using Miki.Bot.Models.Exceptions;
using Miki.Cache;
using Miki.Common.Builders;
using Miki.Configuration;
using Miki.Core.API.Reminder;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Framework.Extension;
using Miki.Models;
using Miki.Rest;
using NCalc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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

		private readonly string[] puns =
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

		private readonly string[] reactions =
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

		private readonly API.TaskScheduler<string> reminders = new API.TaskScheduler<string>();
        private readonly RestClient imageClient;

		public FunModule()
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
                    "cub"
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
                    "shortstack",
                    "larger_male"
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

            if(!string.IsNullOrWhiteSpace(Global.Config.ImageApiUrl))
            {
                imageClient = new RestClient(Global.Config.ImageApiUrl);
            }
		}

		[Command(Name = "8ball")]
		public Task EightBallAsync(ICommandContext e)
		{
			string output = e.Locale.GetString("miki_module_fun_8ball_result",
				e.Author.Username, e.Locale.GetString(reactions[MikiRandom.Next(0, reactions.Length)]));
			e.Channel.QueueMessage(output);
			return Task.CompletedTask;
		}

		[Command(Name = "bird", Aliases = new string[] { "birb" })]
		public async Task BirdAsync(ICommandContext e)
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

			await new EmbedBuilder()
				.SetTitle("🐦 Birbs!")
				.SetColor(0.8f, 0.4f, 0.4f)
				.SetImage(bird[MikiRandom.Next(0, bird.Length)])
				.ToEmbed().QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "cat")]
		public async Task CatAsync(ICommandContext e)
		{
			WebClient c = new WebClient();
			byte[] b = c.DownloadData("http://aws.random.cat/meow");
			string str = Encoding.Default.GetString(b);
			CatImage cat = JsonConvert.DeserializeObject<CatImage>(str);

			await new EmbedBuilder()
				.SetTitle("🐱 Kitties!")
				.SetColor(0.8f, 0.6f, 0.4f)
				.SetImage(cat.File)
				.ToEmbed()
				.QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "compliment")]
		public Task ComplimentAsync(ICommandContext e)
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

			e.Channel.QueueMessage(I_LIKE[MikiRandom.Next(0, I_LIKE.Length)] + BODY_PART[MikiRandom.Next(0, BODY_PART.Length)] + SUFFIX[MikiRandom.Next(0, SUFFIX.Length)]);
			return Task.CompletedTask;
		}

		[Command(Name = "cage")]
		public Task CageAsync(ICommandContext e)
		{
			e.Channel.QueueMessage("http://www.placecage.com/c/" + MikiRandom.Next(100, 1500) + "/" + MikiRandom.Next(100, 1500));
			return Task.CompletedTask;
		}

		[Command(Name = "dog")]
		public async Task DogAsync(ICommandContext e)
		{
            string url;
            do
			{
				url = (await new Rest.RestClient("https://random.dog/woof").GetAsync("")).Body;
			} while (string.IsNullOrEmpty(url) || url.ToLower().EndsWith("mp4"));

			await new EmbedBuilder()
				.SetTitle("🐶 Doggo!")
				.SetColor(0.8f, 0.8f, 0.8f)
				.SetImage("https://random.dog/{url}")
				.ToEmbed().QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "gif")]
		public async Task ImgurGifAsync(ICommandContext e)
		{
            string title = e.Arguments.Pack.TakeAll();
			if (string.IsNullOrEmpty(title))
			{
				e.Channel.QueueMessage(e.Locale.GetString("miki_module_fun_image_error_no_image_found"));
				return;
			}

			var client = new MashapeClient(ImgurClientId, ImgurKey);
			var endpoint = new GalleryEndpoint(client);
			var images = await endpoint.SearchGalleryAsync($"title:{title} ext:gif");
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

				e.Channel.QueueMessage(i.Link);
			}
			else
			{
				e.Channel.QueueMessage(e.Locale.GetString("miki_module_fun_image_error_no_image_found"));
			}
		}

		[Command(Name = "img")]
		public async Task ImgurImageAsync(ICommandContext e)
		{
            string title = e.Arguments.Pack.TakeAll();
            if (string.IsNullOrEmpty(title))
			{
				e.Channel.QueueMessage(e.Locale.GetString("miki_module_fun_image_error_no_image_found"));
				return;
			}

			var client = new MashapeClient(ImgurClientId, ImgurKey);
			var endpoint = new GalleryEndpoint(client);
			var images = await endpoint.SearchGalleryAsync($"title:{title}");
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

				e.Channel.QueueMessage(i.Link);
			}
			else
			{
				e.Channel.QueueMessage(e.Locale.GetString("miki_module_fun_image_error_no_image_found"));
			}
		}

		[Command(Name = "pick")]
		public Task PickAsync(ICommandContext e)
		{
            string args = e.Arguments.Pack.TakeAll();

            if (string.IsNullOrWhiteSpace(args))
			{
				e.Channel.QueueMessage(e.Locale.GetString("miki_module_fun_pick_no_arg"));
				return Task.CompletedTask;
			}
			string[] choices = args.Split(',');

			e.Channel.QueueMessage(e.Locale.GetString("miki_module_fun_pick", new object[] { e.Author.Username, choices[MikiRandom.Next(0, choices.Length)] }));
			return Task.CompletedTask;
		}

		[Command(Name = "pun")]
		public Task PunAsync(ICommandContext e)
		{
			e.Channel.QueueMessage(e.Locale.GetString(puns[MikiRandom.Next(0, puns.Length)]));
			return Task.CompletedTask;
		}

		[Command(Name = "roll")]
		public async Task RollAsync(ICommandContext e)
		{
			string rollResult;
            string args = e.Arguments.Pack.TakeAll();

            if (string.IsNullOrWhiteSpace(args)) // No Arguments.
			{
				rollResult = MikiRandom.Roll(100).ToString();
			}
			else
			{
				if (int.TryParse(args, out int max)) // Simple number argument.
				{
					rollResult = MikiRandom.Roll(max).ToString();
				}
				else // Assume the user has entered an advanced expression.
				{
					Regex regex = new Regex(@"(\d+)?d(\d+)");
					string fullExpression = args;
					int expressionCount = 0;

					foreach (Match match in regex.Matches(args))
					{
						GroupCollection groupCollection = match.Groups;
						int dieCount = groupCollection[1].Success ? int.Parse(groupCollection[1].Value) : 1;
						int dieSides = int.Parse(groupCollection[2].Value);
						List<string> partialExpression = new List<string>();

						for (int i = 0; i < dieCount; i++)
						{
							partialExpression.Add(MikiRandom.Roll(dieSides).ToString());
						}

						fullExpression = regex.Replace(fullExpression, 
							$"({string.Join(" + ", partialExpression)})", 1);
						expressionCount++;
					}

					if (expressionCount > 1)
					{
						fullExpression = $"({fullExpression})";
					}

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

			e.Channel.QueueMessage(e.Locale.GetString("miki_module_fun_roll_result", e.Author.Username, rollResult));
		}

		[Command(Name = "reminder", Aliases = new[] { "remind" })]
		public async Task RemindAsync(ICommandContext e)
		{
			string arguments = e.Arguments.Pack.TakeAll();
            string lowercaseArguments = arguments.ToLower().Split(' ')[0];


            switch (lowercaseArguments)
			{
				case "-clear":
				{
					await CancelReminderAsync(e);
				}
				break;

				case "-list":
				{
					await ListRemindersAsync(e);
				}
				break;

				default:
				{
					if (string.IsNullOrWhiteSpace(lowercaseArguments) || lowercaseArguments.StartsWith("-"))
					{
						await HelpReminderAsync(e);
					}
					else
					{
						await PlaceReminderAsync(e, arguments);
					}
				}
				break;
			}
		}

		private async Task PlaceReminderAsync(ICommandContext e, string args)
		{
			int splitIndex = args.ToLower().LastIndexOf(" in ");
			// TODO: still a bit hacky

			if (splitIndex == -1)
			{
				await e.ErrorEmbed(e.Locale.GetString("error_argument_null", "time"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}

			string reminderText = new string(args
				.Take(splitIndex)
				.ToArray()
			);

			TimeSpan timeUntilReminder = args.GetTimeFromString();

			if (timeUntilReminder > new TimeSpan(0, 10, 0))
			{
				int id = reminders.AddTask(e.Author.Id, async (context) =>
				{
					await new EmbedBuilder()
						.SetTitle("⏰ Reminder")
						.SetDescription(new MessageBuilder()
							.AppendText(context)
							.BuildWithBlockCode())
						.ToEmbed().QueueToChannelAsync(e.Author.GetDMChannelAsync().Result);
				}, reminderText, timeUntilReminder);

				await new EmbedBuilder()
					.SetTitle($"👌 {e.Locale.GetString("term_ok")}")
					.SetDescription($"I'll remind you to **{reminderText}** in **{timeUntilReminder.ToTimeString(e.Locale)}**\nYour reminder code is `{id}`")
					.SetColor(255, 220, 93)
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
			else
			{
				await e.ErrorEmbed("Sorry, but I can only remind you something after 10 minutes.")
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		private async Task CancelReminderAsync(ICommandContext e)
		{
            if (e.Arguments.Take(out string arg))
            {
                if (Utils.IsAll(arg))
                {
                    if (reminders.GetAllInstances(e.Author.Id) is List<TaskInstance<string>> instances)
                    {
                        instances.ForEach(i => i.Cancel());
                    }

                    await new EmbedBuilder()
                        .SetTitle($"⏰ {e.Locale.GetString("reminders")}")
                        .SetColor(0.86f, 0.18f, 0.26f)
                        .SetDescription(e.Locale.GetString("reminder_cancelled_all"))
                        .ToEmbed().QueueToChannelAsync(e.Channel);
                    return;
                }
            }
            else if (e.Arguments.Take(out int id))
            {
                if (reminders.CancelReminder(e.Author.Id, id) is TaskInstance<string> i)
                {
                    await new EmbedBuilder()
                        .SetTitle($"⏰ {e.Locale.GetString("reminders")}")
                        .SetColor(0.86f, 0.18f, 0.26f)
                        .SetDescription(e.Locale.GetString("reminder_cancelled", $"`{i.Context}`"))
                        .ToEmbed().QueueToChannelAsync(e.Channel);
                    return;
                }
            }
			await e.ErrorEmbed(e.Locale.GetString("error_reminder_null"))
				.ToEmbed().QueueToChannelAsync(e.Channel);
		}

		private async Task ListRemindersAsync(ICommandContext e)
		{
			var instances = reminders.GetAllInstances(e.Author.Id);
			if (instances?.Count > 0)
			{
				instances = instances.OrderBy(x => x.Id).ToList();

				EmbedBuilder embed = new EmbedBuilder()
					.SetTitle($"⏰ {e.Locale.GetString("reminders")}")
					.SetColor(0.86f, 0.18f, 0.26f);

				foreach (var x in instances)
				{
					string tx = x.Context;
					string status = "▶";

					if (x.Context.Length > 30)
					{
						tx = new string(x.Context.Take(27).ToArray()) + "...";
					}

					if (x.RepeatReminder)
					{
						status = "🔁";
					}

					embed.Description +=
						$"{status} `{x.Id.ToString().PadRight(3)} - {tx.PadRight(30)} : {x.TimeLeft.ToTimeString(e.Locale, true)}`\n";
				}
				await embed.ToEmbed().QueueToChannelAsync(e.Channel);
			}

			await e.ErrorEmbed(e.Locale.GetString("error_no_reminders"))
				.ToEmbed().QueueToChannelAsync(e.Channel);
		}

        private async Task HelpReminderAsync(ICommandContext e)
        {
            var context = e.GetService<MikiDbContext>();

            string prefix = await e.EventSystem
                    .GetDefaultPrefixTrigger()
                    .GetForGuildAsync(
                        context,
                        e.GetService<ICacheClient>(),
                        e.Guild.Id);

            await new EmbedBuilder()
                .SetTitle($"⏰ {e.Locale.GetString("reminders")}")
                .SetColor(0.86f, 0.18f, 0.26f)
                .SetDescription(e.Locale.GetString("reminder_help_description"))
                .AddInlineField(e.Locale.GetString("term_commands"),
                $"`{prefix}{e.Locale.GetString("reminder_help_add")}` - {e.Locale.GetString("reminder_desc_add")}\n" +
                $"`{prefix}{e.Locale.GetString("reminder_help_clear")}` - {e.Locale.GetString("reminder_desc_clear")}\n" +
                $"`{prefix}{e.Locale.GetString("reminder_help_list")}` - {e.Locale.GetString("reminder_desc_list")}\n")
            .ToEmbed().QueueToChannelAsync(e.Channel);
        }

        [Command(Name = "safe")]
		public async Task DoSafe(ICommandContext e)
		{
			ILinkable s = null;

			if (e.Arguments.Take(out string useArg))
            {
                string tags = e.Arguments.Pack.TakeAll();
                if (useArg.ToLower().StartsWith("use"))
				{
                    switch (useArg.Split(':')[1].ToLower())
					{
						case "safebooru":
						{
							s = await ImageboardProviderPool.GetProvider<SafebooruPost>().GetPostAsync(tags, ImageRating.SAFE);
						}
						break;

						case "gelbooru":
						{
							s = await ImageboardProviderPool.GetProvider<GelbooruPost>().GetPostAsync(tags, ImageRating.SAFE);
						}
						break;

						case "konachan":
						{
							s = await ImageboardProviderPool.GetProvider<KonachanPost>().GetPostAsync(tags, ImageRating.SAFE);
						}
						break;

						case "e621":
						{
							s = await ImageboardProviderPool.GetProvider<E621Post>().GetPostAsync(tags, ImageRating.SAFE);
						}
						break;

						default:
						{
							e.Channel.QueueMessage("I do not support this image host :(");
						}
						break;
					}
				}
				else
				{
					s = await ImageboardProviderPool.GetProvider<SafebooruPost>().GetPostAsync(tags, ImageRating.SAFE);
				}
			}
			else
			{
                string tags = e.Arguments.Pack.TakeAll();
                s = await ImageboardProviderPool.GetProvider<SafebooruPost>().GetPostAsync(tags, ImageRating.SAFE);
			}

			if (s == null)
			{
				await e.ErrorEmbed("We couldn't find an image with these tags!")
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			e.Channel.QueueMessage(s.Url);
		}

		[Command(Name = "ship")]
		public async Task ShipAsync(ICommandContext e)
		{
            var cache = e.GetService<IExtendedCacheClient>();
            var context = e.GetService<MikiDbContext>();

            e.Arguments.Take(out string shipPartner);

			IDiscordGuildUser user = await DiscordExtensions.GetUserAsync(shipPartner, e.Guild);

			if (user == null)
            {
                throw new UserNullException();
            }

            using (var client = new RestClient(Global.Config.CdnRegionEndpoint))
            {
                var authorResponse = await client.SendAsync(new HttpRequestMessage()
                {
                    Method = new HttpMethod("HEAD"),
                    RequestUri = new Uri($"{Global.Config.CdnRegionEndpoint}/avatars/{e.Author.Id}.png")
                });

                if (!authorResponse.Success)
                {
                    await Utils.SyncAvatarAsync(e.Author, cache, context);
                }

                var otherResponse = await client.SendAsync(new HttpRequestMessage()
                {
                    Method = new HttpMethod("HEAD"),
                    RequestUri = new Uri($"{Global.Config.CdnRegionEndpoint}/avatars/{user.Id}.png")
                });

                if (!authorResponse.Success)
                {
                    await Utils.SyncAvatarAsync(e.Author, cache, context);
                }
            }
            Random r = new Random((int)((e.Author.Id + user.Id + (ulong)DateTime.Now.DayOfYear) % int.MaxValue));

			int value = r.Next(0, 100);

			Stream s = await imageClient.GetStreamAsync($"/api/ship?me={e.Author.Id}&other={user.Id}&value={value}");
			await (e.Channel as IDiscordTextChannel).SendFileAsync(s, "meme.png");
		}

		[Command(Name = "greentext", Aliases = new string[] { "green", "gt" })]
		public async Task GreentextAsync(ICommandContext e)
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
				"http://i.imgur.com/g2vdQ6i.jpg",
				"http://i.imgur.com/3vDUWgr.png",
				"http://i.imgur.com/TN58jEQ.jpg",
				"http://i.imgur.com/94wckTB.png"
			};

			await new EmbedBuilder()
				.SetImage(images[MikiRandom.Next(0, images.Length)])
				.ToEmbed().QueueToChannelAsync(e.Channel);
		}
	}
}
