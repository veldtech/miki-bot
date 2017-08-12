using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Extensions;
using IA.SDK.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("Actions")]
    public class ActionsModule
    {
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
            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "cake")]
        public async Task CakeAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://i.imgur.com/CYyrjRQ.gif",
                "http://i.imgur.com/3nWbcNT.gif",
                "http://i.imgur.com/AhOVdff.gif",
                "http://i.imgur.com/QRJ6xqB.gif",
                "http://i.imgur.com/Fuc4BX7.gif",
                "http://i.imgur.com/VQjMsms.gif",
                "http://i.imgur.com/ZwJJzQu.gif",
                "http://i.imgur.com/NupHmFh.gif",
                "http://i.imgur.com/5bnJJKq.gif",
                "http://i.imgur.com/eIMcqa9.gif",
            };

            Random r = new Random();

            IDiscordEmbed embed = Utils.Embed;

            if (e.arguments.Length > 0)
            {
                embed.Title = $"{e.Author.Username} feeds {e.message.RemoveMentions(e.arguments)} cake";
            }
            else
            {
                embed.Title = $"{e.message.Bot.Username} feeds {e.Author.Username} cake";
            }
            embed.ImageUrl = images[r.Next(0, images.Length)];

            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "cuddle")]
        public async Task CuddleAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://i.imgur.com/CEkg7K3.gif",
                "http://i.imgur.com/K4lYduH.gif",
                "http://imgur.com/8kLQ55E.gif",
                "http://i.imgur.com/kd0F5bV.gif",
                "http://imgur.com/zG60zPk.gif",
                "http://i.imgur.com/ct76LIg.gif",
            };

            Random r = new Random();

            IDiscordEmbed embed = Utils.Embed;

            if (e.arguments.Length > 0)
            {
                embed.Title = $"{e.Author.Username} cuddles with {e.message.RemoveMentions(e.arguments)}";
            }
            else
            {
                embed.Title = $"{e.message.Bot.Username} cuddles with {e.Author.Username}";
            }
            embed.ImageUrl = images[r.Next(0, images.Length)];

            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "glare")]
        public async Task GlareAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://i.imgur.com/ba9Skjf.gif",
                "http://i.imgur.com/V6oBWDn.gif",
                "http://i.imgur.com/PWXcVQf.gif",
                "http://i.imgur.com/nOwOSjA.gif",
                "http://i.imgur.com/mG2Hm8s.gif",
                "http://i.imgur.com/iiJCWns.gif",
                "http://i.imgur.com/onUZvOi.gif",
                "http://i.imgur.com/cZwkHOB.gif",
                "http://i.imgur.com/uehetOS.gif",
                "http://i.imgur.com/MAZIl3c.gif",
                "http://i.imgur.com/C1u3GwL.gif",
                "http://i.imgur.com/E7NniAn.gif",
                "http://i.imgur.com/2RKfil2.gif",
                "http://i.imgur.com/jcSpVTS.gif",
                "http://i.imgur.com/r2X5YfC.gif",
                "http://i.imgur.com/qGQry9o.gif",
                "http://i.imgur.com/rRMUuQu.gif",
                "http://i.imgur.com/v47st6k.gif",
                "http://i.imgur.com/iiJCWns.gif",
                "http://i.imgur.com/v47st6k.gif",
            };

            Random r = new Random();

            IDiscordEmbed embed = Utils.Embed
                .SetImageUrl(images[r.Next(0, images.Length)]);

            if (e.arguments.Length > 0)
            {
                embed.Title = $"{e.Author.Username} glares at {e.message.RemoveMentions(e.arguments)}";
            }
            else
            {
                embed.Title = $"{e.message.Bot.Username} glares at {e.Author.Username}";
            }

            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "highfive")]
        public async Task HighFiveAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://imgur.com/LOoXzd9.gif",
                "http://imgur.com/Kwe6pAn.gif",
                "http://imgur.com/JeWzGGl.gif",
                "http://imgur.com/dqVx2oM.gif",
                "http://imgur.com/4n1K6kV.gif",
                "http://imgur.com/206dwM0.gif",
                "http://imgur.com/4ybFKuz.gif",
                "http://imgur.com/21e7SHD.gif",
                "http://imgur.com/LOCVVvL.gif",
                "http://imgur.com/h2KJJUA.gif",
                "http://i.imgur.com/ZUe3F3P.gif",
                "http://i.imgur.com/8xuO60E.gif",
                "http://i.imgur.com/4tMP3wu.gif",
                "http://i.imgur.com/F9odBEE.gif",
                "http://i.imgur.com/U742vH8.gif"
            };

            Random r = new Random();

            IDiscordEmbed embed = Utils.Embed;

            if (e.arguments.Length > 0)
            {
                embed.Title = $"{e.Author.Username} high fives at {e.message.RemoveMentions(e.arguments)}";
            }
            else
            {
                embed.Title = $"{e.message.Bot.Username} high fives at {e.Author.Username}";
            }
            embed.ImageUrl = images[r.Next(0, images.Length)];

            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "hug")]
        public async Task HugAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://imgur.com/FvSnQs8.gif",
                "http://imgur.com/rXEq7oU.gif",
                "http://imgur.com/b6vVMQO.gif",
                "http://imgur.com/KJNTXm3.gif",
                "http://imgur.com/gn18SX8.gif",
                "http://imgur.com/SUdqF9w.gif",
                "http://imgur.com/7C36d39.gif",
                "http://imgur.com/ZOINyyw.gif",
                "http://imgur.com/Imxjcio.gif",
                "http://imgur.com/GNUeLdo.gif",
                "http://imgur.com/K52NZ36.gif",
                "http://imgur.com/683fWwC.gif",
                "http://imgur.com/0RgdLt4.gif",
                "http://imgur.com/jxPPkM8.gif",
                "http://imgur.com/oExwffx.gif",
                "http://imgur.com/pCZpL5h.gif",
                "http://imgur.com/GvQOwuy.gif",
                "http://imgur.com/cLHRyeB.gif",
                "http://imgur.com/FVbzx1A.gif",
                "http://imgur.com/gMLlFNC.gif",
                "http://imgur.com/FOdbhav.gif",
                "http://i.imgur.com/CEkg7K3.gif",
            };

            Random r = new Random();

            IDiscordEmbed embed = Utils.Embed;

            if (e.arguments.Length > 0)
            {
                embed.Title = $"{e.Author.Username} hugs {e.message.RemoveMentions(e.arguments)}";
            }
            else
            {
                embed.Title = $"{e.message.Bot.Username} hugs {e.Author.Username}";
            }
            embed.ImageUrl = images[r.Next(0, images.Length)];

            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "poke")]
        public async Task PokeAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://imgur.com/WG8EKwM.gif",
                "http://imgur.com/dfoxby7.gif",
                "http://imgur.com/TzD1Ngz.gif",
                "http://imgur.com/i1hwvQu.gif",
                "http://imgur.com/bStOFsM.gif",
                "http://imgur.com/1PBeB9H.gif",
                "http://imgur.com/3kerpju.gif",
                "http://imgur.com/uMBRFjX.gif",
                "http://imgur.com/YDJFoBV.gif",
            };

            Random r = new Random();

            IDiscordEmbed embed = Utils.Embed;

            if (e.arguments.Length > 0)
            {
                embed.Title = $"{e.Author.Username} pokes {e.message.RemoveMentions(e.arguments)}";
            }
            else
            {
                embed.Title = $"{e.message.Bot.Username} pokes {e.Author.Username}";
            }
            embed.ImageUrl = images[r.Next(0, images.Length)];

            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "punch")]
        public async Task PunchAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://imgur.com/jVc3GGv.gif",
                "http://imgur.com/iekwz4h.gif",
                "http://imgur.com/AbRmlAo.gif",
                "http://imgur.com/o5MoMYi.gif",
                "http://imgur.com/yNfMX9B.gif",
                "http://imgur.com/bwXvfKE.gif",
                "http://imgur.com/6wKJVHy.gif",
                "http://imgur.com/kokCK1I.gif",
                "http://imgur.com/E3CtvPV.gif",
                "http://imgur.com/q7AmR8n.gif",
                "http://imgur.com/pDohPrm.gif",
            };

            Random r = new Random();

            IDiscordEmbed embed = Utils.Embed;

            if (e.arguments.Length > 0)
            {
                embed.Title = $"{e.Author.Username} punches {e.message.RemoveMentions(e.arguments)}";
            }
            else
            {
                embed.Title = $"{e.message.Bot.Username} punches {e.Author.Username}";
            }
            embed.ImageUrl = images[r.Next(0, images.Length)];

            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "kiss")]
        public async Task KissAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://imgur.com/QIPaYW3.gif",
                "http://imgur.com/wx3WXZu.gif",
                "http://imgur.com/ZzIQwHP.gif",
                "http://imgur.com/z3TEGxp.gif",
                "http://imgur.com/kJEr7Vu.gif",
                "http://imgur.com/IsIR4V0.gif",
                "http://imgur.com/bmeCqLM.gif",
                "http://imgur.com/LBWIJpu.gif",
                "http://imgur.com/p6hNamc.gif",
                "http://imgur.com/PPw83Ug.gif",
                "http://imgur.com/lZ7gAES.gif",
                "http://imgur.com/Bftud8V.gif",
                "http://i.imgur.com/AicG7H6.gif",
                "http://i.imgur.com/ql3FvuU.gif",
            };

            Random r = new Random();

            IDiscordEmbed embed = Utils.Embed;

            if (e.arguments.Length > 0)
            {
                embed.Title = $"{e.Author.Username} kisses {e.message.RemoveMentions(e.arguments)}";
            }
            else
            {
                embed.Title = $"{e.message.Bot.Username} kisses {e.Author.Username}";
            }
            embed.ImageUrl = images[r.Next(0, images.Length)];

            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "pat", Aliases = new string[] { "pet" })]
        public async Task PetAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://imgur.com/Y2DrXtT.gif",
                "http://imgur.com/G7b4OnS.gif",
                "http://imgur.com/nQqH0Xa.gif",
                "http://imgur.com/mCtyWEr.gif",
                "http://imgur.com/Cju6UX3.gif",
                "http://imgur.com/0YkOcUC.gif",
                "http://imgur.com/QxZjpbV.gif",
            };

            Random r = new Random();

            IDiscordEmbed embed = Utils.Embed;

            if (e.arguments.Length > 0)
            {
                embed.Title = $"{e.Author.Username} pats {e.message.RemoveMentions(e.arguments)}";
            }
            else
            {
                embed.Title = $"{e.message.Bot.Username} pats {e.Author.Username}";
            }
            embed.ImageUrl = images[r.Next(0, images.Length)];

            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "slap")]
        public async Task SlapAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://i.imgur.com/GQtzDsV.gif",
                "http://i.imgur.com/rk8eqnt.gif",
                "http://i.imgur.com/UnzGS24.gif",
                "http://i.imgur.com/CHbRGnV.gif",
                "http://i.imgur.com/DvwnC0r.gif",
                "http://i.imgur.com/Ksy8dvd.gif",
                "http://i.imgur.com/b75B4qM.gif",
                "http://i.imgur.com/d9thUdx.gif",
                "http://imgur.com/iekwz4h.gif",
                "http://imgur.com/q7AmR8n.gif",
                "http://imgur.com/pDohPrm.gif",
            };

            Random r = new Random();

            IDiscordEmbed embed = Utils.Embed;

            if (e.arguments.Length > 0)
            {
                embed.Title = $"{e.Author.Username} slaps {e.message.RemoveMentions(e.arguments)}";
            }
            else
            {
                embed.Title = $"{e.message.Bot.Username} slaps {e.Author.Username}";
            }
            embed.ImageUrl = images[r.Next(0, images.Length)];

            await embed.SendToChannel(e.Channel);
        }
    }
}