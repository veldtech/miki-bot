using Miki.Framework.Events.Attributes;
using Miki.Languages;
using System.Threading.Tasks;
using Miki.API.Imageboards;
using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Interfaces;
using Miki.API.Imageboards.Objects;
using Miki.Framework.Events;
using Miki.Framework.Extension;
using System;
using Miki.Discord;
using Miki.Discord.Common;

namespace Miki.Modules
{
	[Module(Name = "nsfw", Nsfw = true)]
	internal class NsfwModule
	{
		[Command(Name = "gelbooru", Aliases = new[] { "gel" })]
		public async Task RunGelbooru(EventContext e)
		{
			try
			{ 
			ILinkable s = await ImageboardProviderPool.GetProvider<GelbooruPost>().GetPostAsync(e.Arguments.ToString(), ImageboardRating.EXPLICIT);

			if (!IsValid(s))
			{
				e.ErrorEmbed("Couldn't find anything with these tags!")
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			CreateEmbed(s)
				.QueueToChannel(e.Channel);
			}
			catch
			{
				e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "danbooru", Aliases = new[] { "dan" })]
		public async Task DanbooruAsync(EventContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<DanbooruPost>().GetPostAsync(e.Arguments.ToString(), ImageboardRating.EXPLICIT);

				if (!IsValid(s))
				{
					e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				CreateEmbed(s)
					.QueueToChannel(e.Channel);
			}
			catch
			{
				e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "rule34", Aliases = new[] { "r34" })]
		public async Task RunRule34(EventContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<Rule34Post>().GetPostAsync(e.Arguments.ToString(), ImageboardRating.EXPLICIT);

				if (!IsValid(s))
				{
					e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				CreateEmbed(s)
					.QueueToChannel(e.Channel);
			}
			catch
			{
				e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "e621")]
		public async Task RunE621(EventContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<E621Post>().GetPostAsync(e.Arguments.ToString(), ImageboardRating.EXPLICIT);

				if (!IsValid(s))
				{
					e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				CreateEmbed(s)
					.QueueToChannel(e.Channel);
			}
			catch
			{
				e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "yandere")]
		public async Task RunYandere(EventContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<YanderePost>().GetPostAsync(e.Arguments.ToString(), ImageboardRating.EXPLICIT);

				if (!IsValid(s))
				{
					e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				CreateEmbed(s).QueueToChannel(e.Channel);
			}
			catch
			{
				e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		private DiscordEmbed CreateEmbed(ILinkable s)
		{
			string url = string.IsNullOrWhiteSpace(s.SourceUrl) ? "https://miki.ai" : s.SourceUrl;

			return Utils.Embed
				.SetAuthor(s.Provider, "https://i.imgur.com/FeRu6Pw.png", url)
				.AddInlineField("Tags", FormatTags(s.Tags))
				.AddInlineField("Score", s.Score)
				.SetImage(s.Url).ToEmbed();
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