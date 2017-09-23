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
                "http://i.imgur.com/sxpETqU.gif",
                "http://i.imgur.com/eIMcqa9.gif",
                "http://i.imgur.com/7Wx5liV.gif",
                "http://i.imgur.com/vvfl1dE.gif",
                "http://i.imgur.com/FRZhiZe.gif",
                "http://i.imgur.com/3gYwmi1.gif",
                "http://i.imgur.com/8JRLiZd.gif",
                "http://i.imgur.com/tZ4vFdG.gif",
                "http://i.imgur.com/QRA92zQ.gif",
                "http://i.imgur.com/fIYP5ns.gif",
                "http://i.imgur.com/QqxuS8Z.gif",
                "http://i.imgur.com/cSyuIzr.gif",
                "http://i.imgur.com/66v1YeA.gif",
                "http://i.imgur.com/utiWU12.gif",
                "http://i.imgur.com/qbm2gpc.gif",
                "http://i.imgur.com/RcvnwDB.gif",
                "http://i.imgur.com/RmCiKjE.gif",
                "http://i.imgur.com/KpXAL1a.gif",
                "http://i.imgur.com/HWYGovk.gif",
                "http://i.imgur.com/U1ODYh3.gif",
                "http://i.imgur.com/YkYHLrX.mp4",
                "http://i.imgur.com/YuGMHo6.gif",
                "http://i.imgur.com/czupsk9.gif",
                "http://i.imgur.com/CCTZA51.gif",
                "http://i.imgur.com/68ihQjk.gif",
                "http://i.imgur.com/UQykz2g.gif",
                "http://i.imgur.com/v6FWLm8.gif",
                "http://i.imgur.com/GdmGcMA.gif",
                "http://i.imgur.com/oyh9W7X.gif",
                "http://i.imgur.com/LPRofzz.gif",
                "http://i.imgur.com/Y6w6CqT.gif",
                "http://i.imgur.com/oi5fVl9.gif",
                "http://i.imgur.com/wTja8ru.mp4",
                "http://i.imgur.com/DBAEo1L.gif",
                "http://i.imgur.com/QSLpOIR.gif"
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
                "http://i.imgur.com/8kLQ55E.gif",
                "http://i.imgur.com/kd0F5bV.gif",
                "http://i.imgur.com/zG60zPk.gif",
                "http://i.imgur.com/ct76LIg.gif",
                "http://i.imgur.com/guBWT22.gif",
                "http://i.imgur.com/Asnv32U.gif"
                
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
                "http://i.imgur.com/VQpxVLE.gif",
                "http://i.imgur.com/uu8cTZO.gif",
                "http://i.imgur.com/i4l9F8R.gif",
                "http://i.imgur.com/BXE2bKM.gif",
                "http://i.imgur.com/PeVwwzy.gif",
                "http://i.imgur.com/lvADpDY.gif",
                "http://i.imgur.com/RovvrqD.gif",
                "http://i.imgur.com/K40NP62.gif",
                "http://i.imgur.com/mC3JYtl.gif",
                "http://i.imgur.com/xQMxKTT.gif",
                "http://i.imgur.com/2hWR6br.gif",
                "http://i.imgur.com/UmhwZSk.gif",
                "http://i.imgur.com/LIgO56g.gif",
                "http://i.imgur.com/hRz09iS.gif",
                "http://i.imgur.com/gBZJx5a.gif",
                "http://i.imgur.com/cq9KBP6.gif",
                "http://i.imgur.com/gIMc3iL.gif",
                "http://i.imgur.com/UIUGfOn.gif",
                "http://i.imgur.com/dNYBTp8.gif",
                "http://i.imgur.com/xgb3wk2.gif",
                "http://i.imgur.com/qzPYYsK.gif"
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
                "http://i.imgur.com/LOoXzd9.gif",
                "http://i.imgur.com/Kwe6pAn.gif",
                "http://i.imgur.com/JeWzGGl.gif",
                "http://i.imgur.com/dqVx2oM.gif",
                "http://i.imgur.com/4n1K6kV.gif",
                "http://i.imgur.com/206dwM0.gif",
                "http://i.imgur.com/4ybFKuz.gif",
                "http://i.imgur.com/21e7SHD.gif",
                "http://i.imgur.com/LOCVVvL.gif",
                "http://i.imgur.com/h2KJJUA.gif",
                "http://i.imgur.com/ZUe3F3P.gif",
                "http://i.imgur.com/8xuO60E.gif",
                "http://i.imgur.com/4tMP3wu.gif",
                "http://i.imgur.com/F9odBEE.gif",
                "http://i.imgur.com/U742vH8.gif",
                "http://i.imgur.com/BSMMYrn.gif",
                "http://i.imgur.com/IuXs0ES.gif",
                "http://i.imgur.com/ip6XWxt.mp4",
                "http://i.imgur.com/Wxl5was.gif",
                "http://i.imgur.com/TPhdaez.gif",
                "http://i.imgur.com/ebQWKZU.gif",
                "http://i.imgur.com/XYA8ET8.gif"
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
                "http://i.imgur.com/FvSnQs8.gif",
                "http://i.imgur.com/rXEq7oU.gif",
                "http://i.imgur.com/b6vVMQO.gif",
                "http://i.imgur.com/KJNTXm3.gif",
                "http://i.imgur.com/gn18SX8.gif",
                "http://i.imgur.com/SUdqF9w.gif",
                "http://i.imgur.com/7C36d39.gif",
                "http://i.imgur.com/ZOINyyw.gif",
                "http://i.imgur.com/Imxjcio.gif",
                "http://i.imgur.com/GNUeLdo.gif",
                "http://i.imgur.com/K52NZ36.gif",
                "http://i.imgur.com/683fWwC.gif",
                "http://i.imgur.com/0RgdLt4.gif",
                "http://i.imgur.com/jxPPkM8.gif",
                "http://i.imgur.com/oExwffx.gif",
                "http://i.imgur.com/pCZpL5h.gif",
                "http://i.imgur.com/GvQOwuy.gif",
                "http://i.imgur.com/cLHRyeB.gif",
                "http://i.imgur.com/FVbzx1A.gif",
                "http://i.imgur.com/gMLlFNC.gif",
                "http://i.imgur.com/FOdbhav.gif",
                "http://i.imgur.com/CEkg7K3.gif",
                "http://i.imgur.com/MrEMpE6.gif",
                "http://i.imgur.com/Y9sMTP4.gif"
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
                "http://i.imgur.com/WG8EKwM.gif",
                "http://i.imgur.com/dfoxby7.gif",
                "http://i.imgur.com/TzD1Ngz.gif",
                "http://i.imgur.com/i1hwvQu.gif",
                "http://i.imgur.com/bStOFsM.gif",
                "http://i.imgur.com/1PBeB9H.gif",
                "http://i.imgur.com/3kerpju.gif",
                "http://i.imgur.com/uMBRFjX.gif",
                "http://i.imgur.com/YDJFoBV.gif",
                "http://i.imgur.com/urC9B1H.gif"
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
                "http://i.imgur.com/QIPaYW3.gif",
                "http://i.imgur.com/wx3WXZu.gif",
                "http://i.imgur.com/ZzIQwHP.gif",
                "http://i.imgur.com/z3TEGxp.gif",
                "http://i.imgur.com/kJEr7Vu.gif",
                "http://i.imgur.com/IsIR4V0.gif",
                "http://i.imgur.com/bmeCqLM.gif",
                "http://i.imgur.com/LBWIJpu.gif",
                "http://i.imgur.com/p6hNamc.gif",
                "http://i.imgur.com/PPw83Ug.gif",
                "http://i.imgur.com/lZ7gAES.gif",
                "http://i.imgur.com/Bftud8V.gif",
                "http://i.imgur.com/AicG7H6.gif",
                "http://i.imgur.com/ql3FvuU.gif",
                "http://i.imgur.com/XLjH6zQ.gif",
                "http://i.imgur.com/hrT0CNB.gifv",
                "http://i.imgur.com/W7arBvy.gif",
                "http://i.imgur.com/W9htMol.gif",
                "http://i.imgur.com/IVOBC8p.gif"
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
                "http://i.imgur.com/Y2DrXtT.gif",
                "http://i.imgur.com/G7b4OnS.gif",
                "http://i.imgur.com/nQqH0Xa.gif",
                "http://i.imgur.com/mCtyWEr.gif",
                "http://i.imgur.com/Cju6UX3.gif",
                "http://i.imgur.com/0YkOcUC.gif",
                "http://i.imgur.com/QxZjpbV.gif",
                "http://i.imgur.com/wXw7IjY.mp4",
                "http://i.imgur.com/0FLNsZX.gif",
                "http://i.imgur.com/nsiyoRQ.gif",
                "http://i.imgur.com/kWDrnc3.gif",
                "http://i.imgur.com/5c0JGlx.gif",
                "http://i.imgur.com/SuU9WQV.gif",
                "http://i.imgur.com/UuYqD7v.gif",
                "http://i.imgur.com/7pposxh.png",
                "http://i.imgur.com/7wZ6s5M.gif",
                "http://i.imgur.com/VuucXay.gif",
                "http://i.imgur.com/pnb1k5P.gif",
                "http://i.imgur.com/cDKGlTX.gif",
                "http://i.imgur.com/JjWLlcz.gif",
                "http://i.imgur.com/4SiEFQq.gif",
                "http://i.imgur.com/JfRGrgw.gif",
                "http://i.imgur.com/HiKI49x.gif",
                "http://i.imgur.com/VBCPpjk.gif",
                "http://i.imgur.com/qL5SShC.gif",
                "http://i.imgur.com/fvgSWgw.gif",
                "http://i.imgur.com/bOrLVXd.gif",
                "http://i.imgur.com/OtcIL4q.jpg",
                "http://i.imgur.com/UwcwNiU.gif",
                "http://i.imgur.com/eGEJV7U.mp4",
                "http://i.imgur.com/I5NJuj9.jpg",
                "http://i.imgur.com/425xfl5.mp4",
                "http://i.imgur.com/Y9iZrGG.gif",
                "http://i.imgur.com/75FpUOd.gif",
                "http://i.imgur.com/fmEFgzX.png",
                "http://i.imgur.com/V2VFPSj.gif",
                "http://i.imgur.com/RFd1Gar.gif",
                "http://i.imgur.com/bgXEKqK.gif",
                "http://i.imgur.com/rMeGX0k.gif",
                "http://i.imgur.com/SpoJHzQ.gif",
                "http://i.imgur.com/wFpK9X2.mp4",
                "http://i.imgur.com/ZCucIDe.gif",
                "http://i.imgur.com/b2dC2pu.gif",
                "http://i.imgur.com/0SBqpld.gif",
                "http://i.imgur.com/FAHxGpn.gif",
                "http://i.imgur.com/Q8i2yZz.gif",
                "http://i.imgur.com/46QOOlu.gif",
                "http://i.imgur.com/XhuyMe4.gif",
                "http://i.imgur.com/7bCb9vc.jpg",
                "http://i.imgur.com/1d9y1s1.gif",
                "http://i.imgur.com/npxQPMH.gif",
                "http://i.imgur.com/VcvVbSb.gif",
                "http://i.imgur.com/G7WpBeD.gif",
                "http://i.imgur.com/V8Sj3d4.jpg",
                "http://i.imgur.com/VMQhPNA.gif",
                "http://i.imgur.com/xbqhigm.gif",
                "http://i.imgur.com/ilc8zXi.gif",
                "http://i.imgur.com/4GgbYst.gif",
                "http://i.imgur.com/1mr4NWL.gif",
                "http://i.imgur.com/wXw7IjY.mp4"
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
                "http://i.imgur.com/iekwz4h.gif",
                "http://i.imgur.com/q7AmR8n.gif",
                "http://i.imgur.com/pDohPrm.gif",
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
