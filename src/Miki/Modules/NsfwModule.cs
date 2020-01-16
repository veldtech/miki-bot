namespace Miki.Modules
{
    using Miki.API.Imageboards;
    using Miki.API.Imageboards.Enums;
    using Miki.API.Imageboards.Interfaces;
    using Miki.API.Imageboards.Objects;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Attributes;
    using Miki.UrbanDictionary;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Miki.Localization;
    using Framework.Extension;
    using Miki.Modules.Accounts.Services;
    using Miki.Services.Achievements;

    [Module("nsfw")]
	internal class NsfwModule
	{
		[Command("gelbooru", "gel")]
		public Task RunGelbooru(IContext e)
            => RunNsfwAsync<GelbooruPost>(e);

        [Command("danbooru", "dan")]
		public Task DanbooruAsync(IContext e)
            => RunNsfwAsync<DanbooruPost>(e);

        [Command("rule34", "r34")]
		public Task RunRule34(IContext e)
            => RunNsfwAsync<Rule34Post>(e);

        [Command("e621")]
		public Task RunE621(IContext e)
            => RunNsfwAsync<E621Post>(e);

        [Command("urban")]
        public async Task UrbanAsync(IContext e)
        {
            if(!e.GetArgumentPack().Pack.CanTake)
            {
                return;
            }

            var api = e.GetService<UrbanDictionaryAPI>();

            var query = e.GetArgumentPack().Pack.TakeAll();
            var searchResult = await api.SearchTermAsync(query);

            if(searchResult == null)
            {
                // TODO (Veld): Something went wrong/No results found.
                return;
            }

            UrbanDictionaryEntry entry = searchResult.Entries
                .FirstOrDefault();

            if(entry == null)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("error_term_invalid"))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            string desc = Regex.Replace(entry.Definition, "\\[(.*?)\\]",
                (x) => $"[{x.Groups[1].Value}]({api.GetUserDefinitionURL(x.Groups[1].Value)})"
            );

            string example = Regex.Replace(entry.Example, "\\[(.*?)\\]",
                (x) => $"[{x.Groups[1].Value}]({api.GetUserDefinitionURL(x.Groups[1].Value)})"
            );

            await new EmbedBuilder()
                .SetAuthor("📚 " + entry.Term, null,
                    "http://www.urbandictionary.com/define.php?term=" + query)
                .SetDescription(e.GetLocale()
                    .GetString("miki_module_general_urban_author", entry.Author))
                .AddField(
                    e.GetLocale().GetString("miki_module_general_urban_definition"), desc, true)
                .AddField(
                    e.GetLocale().GetString("miki_module_general_urban_example"), example, true)
                .AddField(
                    e.GetLocale().GetString("miki_module_general_urban_rating"),
                    $"👍 {entry.ThumbsUp:N0} - 👎 {entry.ThumbsDown:N0}", true)
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("yandere")]
        public Task RunYandere(IContext e) 
            => RunNsfwAsync<YanderePost>(e);

        private async Task RunNsfwAsync<T>(IContext e)
            where T : BooruPost, ILinkable
        {
            try
            {
                ILinkable s = await ImageboardProviderPool.GetProvider<T>()
                    .GetPostAsync(e.GetArgumentPack().Pack.TakeAll(), ImageRating.EXPLICIT);

                if(!IsValid(s))
                {
                    await e.ErrorEmbed("Couldn't find anything with these tags!")
                        .ToEmbed().QueueAsync(e, e.GetChannel());
                    return;
                }

                await CreateEmbed(s)
                    .QueueAsync(e, e.GetChannel());
            }
            catch
            {
                await e.ErrorEmbed("Too many tags for this system. sorry :(")
                    .ToEmbed().QueueAsync(e, e.GetChannel());
            }
            await UnlockLewdAchievementAsync(e, e.GetService<AchievementService>());
        }

        private ValueTask UnlockLewdAchievementAsync(IContext e, AchievementService service)
        {
            if(MikiRandom.Next(100) == 50)
            {
                var lewdAchievement = service.GetAchievementOrDefault(AchievementIds.LewdId);
                return new ValueTask(service.UnlockAsync(e, lewdAchievement, e.GetAuthor().Id));
            }
            return default;
        }

        private DiscordEmbed CreateEmbed(ILinkable s)
            => new EmbedBuilder()
                .SetColor(216, 88, 140)
                .SetAuthor(s.Provider, "https://i.imgur.com/FeRu6Pw.png", "https://miki.ai")
                .AddInlineField("🗒 Tags", FormatTags(s.Tags))
                .AddInlineField("⬆ Score", s.Score)
                .AddInlineField("🔗 Source", $"[click here]({s.Url})")
                .SetImage(s.Url).ToEmbed();

        private static string FormatTags(string tags)
            => string.Join(", ", tags.Split(' ').Select(x => $"`{x}`"));

        private static bool IsValid(ILinkable s)
	        => (s != null) && (!string.IsNullOrWhiteSpace(s.Url));
	}
}