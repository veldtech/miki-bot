using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Extensions;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.Actions
{
    public class Addon : IAddon
    {
        public async Task<IAddonInstance> Create(IAddonInstance addon)
        {
            addon.Name = "Miki.Actions";

            addon.Modules = new List<IModule>()
            {
                new Module(x =>
                {
                    x.Name = "Actions";
                    x.Events = new List<ICommandEvent>()
                    {
                        // cuddle
                        new CommandEvent(command =>
                        {
                            command.Name = "cuddle";
                            command.Metadata = new EventMetadata(
                                "Cuddle with your waifu/husbando <3",
                                "Could not cuddle Q_Q!!!",
                                ">cuddle", ">cuddle [@user]", ">cuddle my pillow"
                                );

                            command.ProcessCommand = async (e, args) =>
                            {
                                string[] images = new string[]
                                {
                                    "https://67.media.tumblr.com/f50da0e6874bc235383fc435e61cd1dc/tumblr_inline_o34yoyT60F1tlr3az_500.gif",
                                    "https://66.media.tumblr.com/tumblr_ly6n2dCYM41rnw7hko1_500.gif",
                                    "http://imgur.com/8kLQ55E.gif",
                                    "https://66.media.tumblr.com/cb6bb8d0da432035722cd96835f20f0f/tumblr_nq2cyx3qYr1sa6ma2o1_500.gif",
                                    "http://i824.photobucket.com/albums/zz162/dovaiv/tumblr_mxbswzL1qT1sq9yswo1_500.gif",
                                    "http://33.media.tumblr.com/510818c33b426e9ba73f809daec3f045/tumblr_n2bye1AaeQ1tv44eho1_500.gif",
                                };

                                Random r = new Random();

                                IDiscordEmbed embed = e.CreateEmbed();

                                if (args.Length > 0)
                                {
                                    embed.Title = $"{e.Author.Username} cuddles with {e.RemoveMentions(args)}";
                                }
                                else
                                {
                                    embed.Title = $"{e.Bot.Username} cuddles with {e.Author.Username}";
                                }
                                embed.ImageUrl = images[r.Next(0, images.Length)];

                                await e.Channel.SendMessage(embed);
                            };
                        }),

                        // glare
                        new CommandEvent(command =>
                        {
                            command.Name = "glare";
                            command.Metadata = new EventMetadata(
                                "Glare at your enemies! That'll teach them!! >n<",
                                "I-i couldn't glare to that cute face.. Sorry :c",
                                ">glare", ">glare [@user]", ">glare at senpai");

                            command.ProcessCommand = async (e, args) =>
                            {
                                string[] images = new string[]
                                {
                                    "http://i.imgur.com/4pD5sX6.gif",
                                    "https://s-media-cache-ak0.pinimg.com/originals/22/b4/0c/22b40c79d48026cf62c95c31428a89a9.gif",
                                    "http://media.tumblr.com/a0a6f568bece2e789ec55304b049eb91/tumblr_inline_nmctxxyNkr1qf6el3.gif",
                                    "http://i.imgur.com/7dQBZ1l.gif",
                                    "http://33.media.tumblr.com/tumblr_m7xqc0bBeb1r56lqu.gif",
                                    "http://static.tumblr.com/3a85b35bec4523001cff5230d241632f/oistman/ABrmmaavy/tumblr_static_tumblr_mchtm5jbjo1rdol9po1_500.gif",
                                    "https://media.giphy.com/media/pKvo8d1PSpOOA/giphy.gif",
                                    "https://m.popkey.co/cd04de/eA1Re.gif",
                                    "https://media.tenor.co/images/386fb4996e952415422e4de3f7ff9273/tenor.gif",
                                    "http://68.media.tumblr.com/585b32983c5e57a30d3509b2e469bb7e/tumblr_ni8qhvXhcm1sr6y44o1_500.gif",
                                    "http://1.bp.blogspot.com/-2aRBZDBrkXA/U0xOOGakkII/AAAAAAAAA-M/0iwPpVvejGE/s1600/GEB08YD.gif",
                                };

                                Random r = new Random();

                                IDiscordEmbed embed = e.CreateEmbed();

                                if (args.Length > 0)
                                {
                                    embed.Title = $"{e.Author.Username} glares at {e.RemoveMentions(args)}";
                                }
                                else
                                {
                                    embed.Title = $"{e.Bot.Username} glares at {e.Author.Username}";
                                }
                                embed.ImageUrl = images[r.Next(0, images.Length)];

                                await e.Channel.SendMessage(embed);
                            };
                        }),

                        // highfive
                        new CommandEvent(command =>
                        {
                            command.Name = "highfive";
                            command.Metadata = new EventMetadata(
                                "up top, down low too slow! High five your friends with this cool command",
                                "Couldn't high five.",
                                ">highfive", ">highfive [@user]", ">highfive this melon");
                            command.ProcessCommand = async (e, args) =>
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
                                };

                                Random r = new Random();

                                IDiscordEmbed embed = e.CreateEmbed();

                                if (args.Length > 0)
                                {
                                    embed.Title = $"{e.Author.Username} high fives at {e.RemoveMentions(args)}";
                                }
                                else
                                {
                                    embed.Title = $"{e.Bot.Username} high fives at {e.Author.Username}";
                                }
                                embed.ImageUrl = images[r.Next(0, images.Length)];

                                await e.Channel.SendMessage(embed);
                            };
                        }),

                        // hug
                        new CommandEvent(command =>
                        {
                            command.Name = "hug";
                            command.Metadata = new EventMetadata(
                                "hugs are great, spread hugs with this >hug command!",
                                "Couldn't hug ;-;",
                                ">hug", ">hug [@user]", ">hug with my pillow");
                            command.ProcessCommand = async (e, args) =>
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
                                };

                                Random r = new Random();

                                IDiscordEmbed embed = e.CreateEmbed();

                                if (args.Length > 0)
                                {
                                    embed.Title = $"{e.Author.Username} hugs {e.RemoveMentions(args)}";
                                }
                                else
                                {
                                    embed.Title = $"{e.Bot.Username} hugs {e.Author.Username}";
                                }
                                embed.ImageUrl = images[r.Next(0, images.Length)];

                                await e.Channel.SendMessage(embed);
                            };
                        }),

                        // poke
                        new CommandEvent(command =>
                        {
                            command.Name = "poke";
                            command.Metadata = new EventMetadata(
                                "Is someone not paying attention to you? Poke them to force attention!",
                                "Couldn't poke?",
                                ">poke", ">poke [@user]", ">poke sleeping friend");
                            command.ProcessCommand = async (e, args) =>
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

                                IDiscordEmbed embed = e.CreateEmbed();

                                if (args.Length > 0)
                                {
                                    embed.Title = $"{e.Author.Username} pokes {e.RemoveMentions(args)}";
                                }
                                else
                                {
                                    embed.Title = $"{e.Bot.Username} pokes {e.Author.Username}";
                                }
                                embed.ImageUrl = images[r.Next(0, images.Length)];

                                await e.Channel.SendMessage(embed);
                            };
                        }),

                        // punch
                        new CommandEvent(command =>
                        {
                            command.Name = "punch";
                            command.Metadata = new EventMetadata(
                                "Someone being a jerk? Punch them. hard.",
                                "Couldn't punch this person, too stronk",
                                ">punch", ">punch [@user]", ">punch that one jerk across the street");
                            command.ProcessCommand = async (e, args) =>
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

                                IDiscordEmbed embed = e.CreateEmbed();

                                if (args.Length > 0)
                                {
                                    embed.Title = $"{e.Author.Username} punches {e.RemoveMentions(args)}";
                                }
                                else
                                {
                                    embed.Title = $"{e.Bot.Username} punches {e.Author.Username}";
                                }
                                embed.ImageUrl = images[r.Next(0, images.Length)];

                                await e.Channel.SendMessage(embed);
                            };
                        }),

                        // kiss
                        new CommandEvent(command =>
                        {
                            command.Name = "kiss";
                            command.Metadata = new EventMetadata(
                                "Kiss the person you love the most <3",
                                "Couldn't kiss",
                                ">kiss", ">kiss [@user]", ">kiss this cactus");

                            command.ProcessCommand = async (e, args) =>
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
                                };

                                Random r = new Random();

                                IDiscordEmbed embed = e.CreateEmbed();

                                if (args.Length > 0)
                                {
                                    embed.Title = $"{e.Author.Username} kisses {e.RemoveMentions(args)}";
                                }
                                else
                                {
                                    embed.Title = $"{e.Bot.Username} kisses {e.Author.Username}";
                                }
                                embed.ImageUrl = images[r.Next(0, images.Length)];

                                await e.Channel.SendMessage(embed);
                            };
                        }),

                        // pat
                        new CommandEvent(command =>
                        {
                            command.Name = "pat";
                            command.ProcessCommand = async (e, args) =>
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

                                IDiscordEmbed embed = e.CreateEmbed();

                                if (args.Length > 0)
                                {
                                    embed.Title = $"{e.Author.Username} pats {e.RemoveMentions(args)}";
                                }
                                else
                                {
                                    embed.Title = $"{e.Bot.Username} pats {e.Author.Username}";
                                }
                                embed.ImageUrl = images[r.Next(0, images.Length)];

                                await e.Channel.SendMessage(embed);
                            };
                        }),

                        // pet
                        new CommandEvent(command =>
                        {
                            command.Name = "pet";
                            command.ProcessCommand = async (e, args) =>
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

                                IDiscordEmbed embed = e.CreateEmbed();

                                if (args.Length > 0)
                                {
                                    embed.Title = $"{e.Author.Username} pets {e.RemoveMentions(args)}";
                                }
                                else
                                {
                                    embed.Title = $"{e.Bot.Username} pets {e.Author.Username}";
                                }
                                embed.ImageUrl = images[r.Next(0, images.Length)];

                                await e.Channel.SendMessage(embed);
                            };
                        }),

                        // slap
                        new CommandEvent(command =>
                        {
                            command.Name = "slap";
                            command.ProcessCommand = async (e, args) =>
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

                                IDiscordEmbed embed = e.CreateEmbed();

                                if (args.Length > 0)
                                {
                                    embed.Title = $"{e.Author.Username} slaps {e.RemoveMentions(args)}";
                                }
                                else
                                {
                                    embed.Title = $"{e.Bot.Username} slaps {e.Author.Username}";
                                }
                                embed.ImageUrl = images[r.Next(0, images.Length)];

                                await e.Channel.SendMessage(embed);
                            };
                        }),

                        // cake
                        new CommandEvent(command =>
                        {
                            command.Name = "cake";
                            command.ProcessCommand = async (e, args) =>
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
                                };

                                Random r = new Random();

                                IDiscordEmbed embed = e.CreateEmbed();

                                if (args.Length > 0)
                                {
                                    embed.Title = $"{e.Author.Username} feeds {e.RemoveMentions(args)} cake";
                                }
                                else
                                {
                                    embed.Title = $"{e.Bot.Username} feeds {e.Author.Username} cake";
                                }
                                embed.ImageUrl = images[r.Next(0, images.Length)];

                                await e.Channel.SendMessage(embed);
                            };
                        })
                    };
                })
            };

            return addon;
        }
    }
}
