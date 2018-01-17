using IA.Events.Attributes;
using IA.SDK.Events;
using Miki.Languages;
using System.Threading.Tasks;
using Miki.API.Imageboards;
using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Interfaces;
using Miki.API.Imageboards.Objects;
using IA.SDK.Interfaces;

namespace Miki.Modules
{
	[Module(Name = "nsfw", Nsfw = true)]
	internal class NsfwModule
	{
		[Command(Name = "gelbooru", Aliases = new[] { "gel" })]
		public async Task RunGelbooru(EventContext e)
		{
			ILinkable s = ImageboardProviderPool.GetProvider<GelbooruPost>().GetPost(e.arguments, ImageboardRating.EXPLICIT);

			if (!IsValid(s))
			{
				await Utils.ErrorEmbed(Locale.GetEntity(e.Channel.Id), "Couldn't find anything with these tags!")
					.SendToChannel(e.Channel);
				return;
			}

			await CreateEmbed(s)
				.SendToChannel(e.Channel);
		}

		[Command(Name = "rule34", Aliases = new[] { "r34" })]
		public async Task RunRule34(EventContext e)
		{
			ILinkable s = ImageboardProviderPool.GetProvider<Rule34Post>().GetPost(e.arguments, ImageboardRating.EXPLICIT);

			if (!IsValid(s))
			{
				await Utils.ErrorEmbed(Locale.GetEntity(e.Channel.Id), "Couldn't find anything with these tags!")
					.SendToChannel(e.Channel);
				return;
			}

			await CreateEmbed(s)
				.SendToChannel(e.Channel);
		}

		[Command(Name = "e621")]
		public async Task RunE621(EventContext e)
		{
			ILinkable s = ImageboardProviderPool.GetProvider<E621Post>().GetPost(e.arguments, ImageboardRating.EXPLICIT);

			if (!IsValid(s))
			{
				await Utils.ErrorEmbed(Locale.GetEntity(e.Channel.Id), "Couldn't find anything with these tags!")
					.SendToChannel(e.Channel);
				return;
			}

			await CreateEmbed(s)
				.SendToChannel(e.Channel);
		}

		[Command(Name = "yandere")]
		public async Task RunYandere(EventContext e)
		{
			ILinkable s = ImageboardProviderPool.GetProvider<YanderePost>().GetPost(e.arguments, ImageboardRating.EXPLICIT);

			if (!IsValid(s))
			{
				await Utils.ErrorEmbed(Locale.GetEntity(e.Channel.Id), "Couldn't find anything with these tags!")
					.SendToChannel(e.Channel);
				return;
			}

			await CreateEmbed(s)
				.SendToChannel(e.Channel);
		}

		private IDiscordEmbed CreateEmbed(ILinkable s)
		{
			string url = string.IsNullOrWhiteSpace(s.SourceUrl) ? "https://miki.ai" : s.SourceUrl;

			return Utils.Embed
				.SetAuthor(s.Provider, "https://i.imgur.com/FeRu6Pw.png", url)
				.AddInlineField("Tags", FormatTags(s.Tags))
				.AddInlineField("Score", s.Score)
				.SetImageUrl(s.Url);
		}

		private string FormatTags(string Tags)
		{
			string[] allTags = Tags.Split(' ');
			for (int i = 0; i < allTags.Length; i++)
			{
				allTags[i] = "`" + allTags[i] + "`";
			}
			return string.Join(", ", allTags);
		}

		private bool IsValid(ILinkable s)
		{
			return (s != null) && (!string.IsNullOrWhiteSpace(s.Url));
		}
	}
}