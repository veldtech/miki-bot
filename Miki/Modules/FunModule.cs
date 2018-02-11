using Miki.Framework;
using Miki.Framework.Events.Attributes;
using Miki.Framework.Extension;
using Miki.Common;
using Miki.Common.Events;
using Miki.Common.Interfaces;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using Miki.Accounts.Achievements;
using Miki.Accounts.Achievements.Objects;
using Miki.API;
using Miki.API.Imageboards;
using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Interfaces;
using Miki.Languages;
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
using Miki.Framework.Events;
using Miki.API.Imageboards.Objects;
using Miki.API.Reminder;
using Miki.Core.API.Reminder;

namespace Miki.Modules
{
    [Module(Name = "Fun")]
    public class FunModule
    {
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
        private string[] lunchposts =
{
            "https://soundcloud.com/ghostcoffee-342990942/woof-woof-whats-for-lunch?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/lunchpost-1969?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/meMiki.Frameworkn-alien?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/falcon-lunch?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/antique-lunch-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/eternal-bark-engine-shall-we-feast?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/wuff-wuff-whats-for-lunch?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/in-this-woof-monochrome-lunch-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/dogtone?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/ufo-romance-in-the-nut-sky-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/necromastiff?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/the-dumbest-one-on-the-album?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/reach-fur-the-lunch-immurrtal-goat-from-psydo?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/pure-furries-whereabouts-of-the-lunch-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/pawdemic-picnic-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/moon-pup-homunculus-lunch-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/ancient-pups-song-firepsy-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/tummy-rumbling?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/fantasy-nation-lunchbreak-pupper-prayer-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/feast-of-the-crysanthemum-canine?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/yin-yang-shiba-serpent?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/yin-yang-shiba-serpent-standalone?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/present-world-oppahaul-lunch-mix?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/food-circulating-melody-native-lunch-owo-remix?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/a-lunch-sample",
            "https://soundcloud.com/ghostcoffee-342990942/something-neato-my-dood",
            "https://soundcloud.com/ghostcoffee-342990942/the-best-one",
            "https://soundcloud.com/ghostcoffee-342990942/pure-gentlemen-whereabouts-of-the-style",
            "https://soundcloud.com/ghostcoffee-342990942/scooby-goo",
            "https://soundcloud.com/ghostcoffee-342990942/lunchstep",
            "https://soundcloud.com/ghostcoffee-342990942/antique-lunch",
            "https://soundcloud.com/ghostcoffee-342990942/take-on-lunch",
            "https://soundcloud.com/ghostcoffee-342990942/bonus-chief-keef-lunchus",
            "https://soundcloud.com/ghostcoffee-342990942/lunch-signal",
            "https://soundcloud.com/ghostcoffee-342990942/silent-woof-2",
            "https://soundcloud.com/ghostcoffee-342990942/wild-lunch",
            "https://soundcloud.com/ghostcoffee-342990942/equivilant-1",
            "https://soundcloud.com/ghostcoffee-342990942/exchange-1",
            "https://soundcloud.com/ghostcoffee-342990942/gangnam-woof-1",
            "https://soundcloud.com/ghostcoffee-342990942/hourai-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/lord-of-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/lunchvril-14th-1",
            "https://soundcloud.com/ghostcoffee-342990942/making-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/megalunchovanMiki.Framework",
            "https://soundcloud.com/ghostcoffee-342990942/midnight-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/say-whats-for-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/silent-woof-1",
            "https://soundcloud.com/ghostcoffee-342990942/stop-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/supah-woof-bros-3",
            "https://soundcloud.com/ghostcoffee-342990942/the-worst-one-1",
            "https://soundcloud.com/ghostcoffee-342990942/tnlunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/w-o-o-f-w-a-v-e-1",
            "https://soundcloud.com/ghostcoffee-342990942/whats-for-woof-1",
            "https://soundcloud.com/ghostcoffee-342990942/woofing-in-the-90s-1",
            "https://soundcloud.com/ghostcoffee-342990942/woofline-bling-1"
};

		private ReminderAPI reminders = new ReminderAPI();

        public FunModule(RuntimeModule m)
        {
            ImageboardProviderPool.AddProvider(new ImageboardProvider<E621Post>(new ImageboardConfigurations
            {
               QueryKey = "http://e621.net/post/index.json?limit=1&tags=",
               ExplicitTag = "rating:e",
               QuestionableTag = "rating:q",
               SafeTag = "rating:s",
               NetUseCredentials = true,
               NetHeaders = new List<string>() { "User-Agent: Other" }
            }));
            ImageboardProviderPool.AddProvider(new ImageboardProvider<GelbooruPost>("http://gelbooru.com/index.php?page=dapi&s=post&q=index&json=1&tags="));
            ImageboardProviderPool.AddProvider(new ImageboardProvider<SafebooruPost>("https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&tags="));
            ImageboardProviderPool.AddProvider(new ImageboardProvider<Rule34Post>("http://rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags="));
            ImageboardProviderPool.AddProvider(new ImageboardProvider<KonachanPost>("https://konachan.com/post.json?tags="));
            ImageboardProviderPool.AddProvider(new ImageboardProvider<YanderePost>("https://yande.re/post.json?tags="));
        }

        [Command(Name = "8ball")]
        public async Task EightBallAsync(EventContext e)
        {
            Locale l = Locale.GetEntity(e.Channel.Id.ToDbLong());

            string output = l.GetString("miki_module_fun_8ball_result", new object[] { e.Author.Username, l.GetString(reactions[MikiRandom.Next(0, reactions.Length)]) });
            await e.Channel.QueueMessageAsync(output);
        }

        [Command(Name = "bird")]
        public async Task BirdAsync(EventContext e)
        {
            string[] bird =
            {
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
			await Utils.Embed
			.SetTitle("🐦 Birbs!")
			.SetColor(0.8f, 0.4f, 0.4f)
			.SetImageUrl(bird[MikiRandom.Next(0, bird.Length)])
			.QueueToChannel(e.Channel);
        }

        [Command(Name = "cat")]
        public async Task CatAsync(EventContext e)
        {
            WebClient c = new WebClient();
            byte[] b = c.DownloadData("http://random.cat/meow");
            string str = Encoding.Default.GetString(b);
            CatImage cat = JsonConvert.DeserializeObject<CatImage>(str);

			await Utils.Embed
				.SetTitle("🐱 Kitties!")
				.SetColor(0.8f, 0.6f, 0.4f)
				.SetImageUrl(cat.File)
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
                "your creepy finger in your left hand",
                "your cute smile",
                "those dazzling eyes of yours",
                "your creepy finger in your right hand",
                "the specMiki.Frameworkl angles your elbows makes",
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

            await e.Channel.QueueMessageAsync(I_LIKE[MikiRandom.Next(0, I_LIKE.Length)] + BODY_PART[MikiRandom.Next(0, BODY_PART.Length)] + SUFFIX[MikiRandom.Next(0, SUFFIX.Length)]);
        }

        [Command(Name = "cage")]
        public async Task CageAsync(EventContext e)
        {
            await e.Channel.QueueMessageAsync("http://www.placecage.com/c/" + MikiRandom.Next(100, 1500) + "/" + MikiRandom.Next(100, 1500));
        }

        [Command(Name = "dog")]
        public async Task DogAsync(EventContext e)
        {
			string url = "";

			do
			{
				url = (await new Rest.RestClient("https://random.dog/woof").GetAsync("")).Data;
			} while (string.IsNullOrEmpty(url) || url.ToLower().EndsWith("mp4"));

			await Utils.Embed
				.SetTitle("🐶 Doggo!")
				.SetColor(0.8f, 0.8f, 0.8f)
				.SetImageUrl("https://random.dog/" + url)
				.QueueToChannel(e.Channel);
        }

        [Command(Name = "gif")]
        public async Task ImgurGifAsync(EventContext e)
        {
			if (string.IsNullOrEmpty(e.arguments))
			{
				await e.Channel.QueueMessageAsync(Locale.GetEntity(e.Channel.Id.ToDbLong()).GetString(Locale.ImageNotFound));
				return;
			}

			var client = new MashapeClient(Global.config.ImgurClientId, Global.config.ImgurKey);
            var endpoint = new GalleryEndpoint(client);
            var images = await endpoint.SearchGalleryAsync($"title:{e.arguments} ext:gif");
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

                await e.Channel.QueueMessageAsync(i.Link);
            }
            else
            {
                await e.Channel.QueueMessageAsync(Locale.GetEntity(e.Channel.Id.ToDbLong()).GetString(Locale.ImageNotFound));
            }
        }

        [Command(Name = "img")]
        public async Task ImgurImageAsync(EventContext e)
        {
			if (string.IsNullOrEmpty(e.arguments))
			{
				await e.Channel.QueueMessageAsync(Locale.GetEntity(e.Channel.Id.ToDbLong()).GetString(Locale.ImageNotFound));
				return;
			}

			var client = new MashapeClient(Global.config.ImgurClientId, Global.config.ImgurKey);
            var endpoint = new GalleryEndpoint(client);
            var images = await endpoint.SearchGalleryAsync($"title:{e.arguments}");
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

                await e.Channel.QueueMessageAsync(i.Link);
            }
            else
            {
                await e.Channel.QueueMessageAsync(Locale.GetEntity(e.Channel.Id.ToDbLong()).GetString(Locale.ImageNotFound));
            }
        }

        [Command(Name = "lunch")]
        public async Task LunchAsync(EventContext e)
        {
            await e.Channel.QueueMessageAsync(e.GetResource("lunch_line") + "\n" + lunchposts[MikiRandom.Next(0, lunchposts.Length)]);
        }

        [Command(Name = "pick")]
        public async Task PickAsync(EventContext e)
        {
            if (string.IsNullOrWhiteSpace(e.arguments))
            {
                await e.Channel.QueueMessageAsync(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(Locale.ErrorPickNoArgs));
                return;
            }
            string[] choices = e.arguments.Split(',');

            Locale locale = e.Channel.GetLocale();
            await e.Channel.QueueMessageAsync(locale.GetString(Locale.PickMessage, new object[] { e.Author.Username, choices[MikiRandom.Next(0, choices.Length)] }));
        }

        [Command(Name = "pun")]
        public async Task PunAsync(EventContext e)
        {
            await e.Channel.QueueMessageAsync(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(puns[MikiRandom.Next(0, puns.Length)]));
        }

		[Command(Name = "roll")]
		public async Task RollAsync(EventContext e)
		{
			string rollResult;

			if (string.IsNullOrWhiteSpace(e.arguments)) // No Arguments.
			{
				rollResult = MikiRandom.Roll(100).ToString();
			}
			else
			{
				if (int.TryParse(e.arguments, out int max)) // Simple number argument.
				{
					rollResult = MikiRandom.Roll(max).ToString();
				}
				else // Assume the user has entered an advanced expression.
				{
					Regex regex = new Regex(@"(?<dieCount>\d+)d(?<dieSides>\d+)");
					string fullExpression = e.arguments;
					int expressionCount = 0;

					foreach (Match match in regex.Matches(e.arguments))
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

			await e.Channel.QueueMessageAsync(e.GetResource(Locale.RollResult, e.Author.Username, rollResult));
		}
		
		[Command( Name = "roulette" )]
		public async Task RouletteAsync( EventContext e )
		{
			IEnumerable<IDiscordUser> users = await e.Channel.GetUsersAsync();
			List<IDiscordUser> realUsers = users.Where( user => !user.IsBot ).ToList();

			string mention = "<@" + realUsers[MikiRandom.Next( 0, realUsers.Count )].Id + ">";
			string send = string.IsNullOrEmpty( e.arguments ) ?
				e.GetResource( Locale.RouletteMessageNoArg, mention) :      
				e.GetResource( Locale.RouletteMessage, e.arguments, mention );

			await e.Channel.QueueMessageAsync( send );
		}

        [Command(Name = "reminder", Aliases = new[] { "remind" })]
        public async Task RemindAsync(EventContext e)
        {
			string lowercaseArguments = e.arguments.ToLower().Split(' ')[0];

			switch (lowercaseArguments)
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
					if (string.IsNullOrWhiteSpace(e.arguments) || e.arguments.StartsWith("-"))
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
			Locale locale = e.Channel.GetLocale();

			int inIndex = e.arguments.ToLower().LastIndexOf(" in ");
			int everyIndex = e.arguments.ToLower().LastIndexOf(" every ");
			bool isIn = (inIndex > everyIndex);
			bool repeated = false;

			int splitIndex = isIn ? inIndex : everyIndex;

			if (splitIndex == -1)
			{
				await e.ErrorEmbed(locale.GetString("error_argument_null", "time"))
					.QueueToChannel(e.Channel);
				return;
			}

			if(!isIn)
			{
				repeated = true;
			}

			string reminderText = new string(e.arguments
				.Take(splitIndex)
				.ToArray()
			);

			TimeSpan timeUntilReminder = e.arguments.GetTimeFromString();

			if (timeUntilReminder > new TimeSpan(0, 0, 10))
			{
				int id = reminders.AddReminder(e.Author, reminderText, timeUntilReminder, repeated);

				await Utils.Embed
					.SetTitle($"👌 {locale.GetString("term_ok")}")
					.SetDescription($"I'll remind you to **{reminderText}** {(repeated ? "every" : "in")} **{timeUntilReminder.ToTimeString(e.Channel.GetLocale())}**\nYour reminder code is `{id}`")
					.SetColor(255, 220, 93)
					.QueueToChannel(e.Channel.Id);
			}
			else
			{
				await e.ErrorEmbed("Sorry, but I can only remind you something after 10 minutes.")
					.QueueToChannel(e.Channel);
			}
		}

		private async Task CancelReminderAsync(EventContext e)
		{
			Locale locale = e.Channel.GetLocale();
			string[] args = e.arguments.Split(' ');

			if (args.Length > 1)
			{
				string x = args[1];
				if (Utils.IsAll(x, locale))
				{
					if (reminders.GetAllInstances(e.Author) is List<ReminderInstance> instances)
					{
						instances.ForEach(i => i.Cancel());
					}

					await Utils.Embed
						.SetTitle($"⏰ {locale.GetString("reminders")}")
						.SetColor(0.86f, 0.18f, 0.26f)
						.SetDescription(locale.GetString("reminder_cancelled_all"))
						.QueueToChannel(e.Channel);
					return;
				}
				else if (int.TryParse(x, out int id))
				{
					if (reminders.CancelReminder(e.Author, id) is ReminderInstance i)
					{
						await Utils.Embed
							.SetTitle($"⏰ {locale.GetString("reminders")}")
							.SetColor(0.86f, 0.18f, 0.26f)
							.SetDescription(locale.GetString("reminder_cancelled", $"`{i.Text}`"))
							.QueueToChannel(e.Channel);
						return;
					}
				}
				await e.ErrorEmbed(locale.GetString("error_reminder_null"))
					.QueueToChannel(e.Channel);
			}
			else
			{
				await e.ErrorEmbed(locale.GetString("error_argument_null", "id"))
					.QueueToChannel(e.Channel);
			}
		}
		private async Task ListRemindersAsync(EventContext e)
		{
			Locale locale = e.Channel.GetLocale();

			var instances = reminders.GetAllInstances(e.Author);
			if(instances?.Count > 0)
			{
				instances = instances.OrderBy(x => x.ReminderId).ToList();

				IDiscordEmbed embed = Utils.Embed
					.SetTitle($"⏰ {locale.GetString("reminders")}")
					.SetColor(0.86f, 0.18f, 0.26f);

				foreach (var x in instances)
				{
					string tx = x.Text;
					string status = "▶";

					if (x.Text.Length > 30)
					{
						tx = new string(x.Text.Take(27).ToArray()) + "...";
					}

					if(x.RepeatReminder)
					{
						status = "🔁";
					}

					embed.Description += $"{status} `{x.ReminderId.ToString().PadRight(3)} - {tx.PadRight(30)} : {x.TimeLeft.ToTimeString(Locale.GetEntity(e.Channel.Id), true)}`\n";
				}
				await embed.QueueToChannel(e.Channel);
				return;
			}

			await e.ErrorEmbed(locale.GetString("error_no_reminders"))
				.QueueToChannel(e.Channel);
		}

		private async Task HelpReminderAsync(EventContext e)
		{
			Locale locale = e.Channel.GetLocale();
			string prefix = await e.commandHandler.GetPrefixAsync(e.Guild.Id);

			await Utils.Embed
				.SetTitle($"⏰ {locale.GetString("reminders")}")
				.SetColor(0.86f, 0.18f, 0.26f)
				.SetDescription(locale.GetString("reminder_help_description"))
				.AddInlineField(locale.GetString("term_commands"), 
				$"`{prefix}{locale.GetString("reminder_help_add")}` - {locale.GetString("reminder_desc_add")}\n" +
				$"`{prefix}{locale.GetString("reminder_help_clear")}` - {locale.GetString("reminder_desc_clear")}\n" +
				$"`{prefix}{locale.GetString("reminder_help_list")}` - {locale.GetString("reminder_desc_list")}\n")
				.QueueToChannel(e.Channel);
		}

        [Command(Name = "safe")]
        public async Task DoSafe(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            ILinkable s = null;
            if (e.arguments.ToLower().StartsWith("use"))
            {
                string[] a = e.arguments.Split(' ');
                e.arguments = e.arguments.Substring(a[0].Length);
				switch (a[0].Split(':')[1].ToLower())
				{
					case "safebooru":
					{
						s = ImageboardProviderPool.GetProvider<SafebooruPost>().GetPost(e.arguments, ImageboardRating.SAFE);
					} break;

					case "gelbooru":
					{
						s = ImageboardProviderPool.GetProvider<GelbooruPost>().GetPost(e.arguments, ImageboardRating.SAFE);
					} break;

					case "konachan":
					{
						s = ImageboardProviderPool.GetProvider<KonachanPost>().GetPost(e.arguments, ImageboardRating.SAFE);
					} break;

					case "e621":
					{
						s = ImageboardProviderPool.GetProvider<E621Post>().GetPost(e.arguments, ImageboardRating.SAFE);
					} break;

					default:
					{
						await e.Channel.QueueMessageAsync("I do not support this image host :(");
					} break;
				}
            }
            else
            {
                s = ImageboardProviderPool.GetProvider<SafebooruPost>().GetPost(e.arguments, ImageboardRating.SAFE);
            }

            if (s == null)
            {
                await Utils.ErrorEmbed(locale, "We couldn't find an image with these tags!").QueueToChannel(e.Channel);
                return;
            }

            await e.Channel.QueueMessageAsync(s.Url);
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

            RuntimeEmbed em = new RuntimeEmbed( new Discord.EmbedBuilder() )
            {
                ImageUrl = images[MikiRandom.Next( 0, images.Length )]
            };
            await em.QueueToChannel( e.Channel );
        }
    }
}
