using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Extensions;
using IA.SDK.Interfaces;
using Miki.API.Patreon;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("patreon")]
    internal class PatreonModule
    {
        private string[] lunchposts = new string[]
        {
            "https://soundcloud.com/ghostcoffee-342990942/woof-woof-whats-for-lunch?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/lunchpost-1969?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/meian-alien?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
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
            "https://soundcloud.com/ghostcoffee-342990942/megalunchovania",
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

        [Command(Name = "ask")]
        public async Task AskAsync(EventContext e)
        {
            string image = "http://i.imgur.com/AHPnL.gif";
            IDiscordEmbed embed = Utils.Embed;

            embed.Description = $"{e.Author.Username} asks {e.message.RemoveMentions(e.arguments)}";

            if (e.message.MentionedUserIds.Count > 0)
            {
                if (e.Author.Id == 114190551670194183 && e.message.MentionedUserIds.First() == 185942988596183040)
                {
                    IDiscordUser u = await e.Guild.GetUserAsync(185942988596183040);
                    image = "http://i.imgur.com/AFcG8LU.gif";
                    embed.Description = $"{e.Author.Username} asks {u.Username} for lewds";
                }
            }

            embed.ImageUrl = image;
            await e.Channel.SendMessage(embed);
    }

        [Command(Name = "lunch")]
        public async Task LunchAsync(EventContext e)
        {
            await e.Channel.SendMessage("Woof woof! What's for lunch?\n" + lunchposts[Global.random.Next(0, lunchposts.Length)]);
        }
    }
}