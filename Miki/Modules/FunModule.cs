using Discord;
using Meru;
using Meru.Events;
using Meru.SDK;
using Meru.SDK.Events;
using Meru.SDK.Interfaces;
using IMDBNet;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.Languages;
using Miki.Models;
using Miki.Objects;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    public class FunModule
    {
        public async Task LoadEvents(Client bot)
        {
            string[] puns = new string[]
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
            string[] reactions = new string[]
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

            IModule module_fun = new Module(module =>
            {
                module.Name = "fun";
                module.Events = new List<ICommandEvent>()
                {
                    new CommandEvent(x =>
                    {
                        x.Name = "8ball";
                        x.ProcessCommand = async (e, args) =>
                        {
                            Locale l = Locale.GetEntity(e.Guild.Id.ToDbLong());

                            string output = l.GetString("miki_module_fun_8ball_result", new object[] { e.Author.Username, l.GetString(reactions[Global.random.Next(0, reactions.Length)]) });
                            await e.Channel.SendMessage(output);
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "bird";
                          x.ProcessCommand = async (e, args) =>
                          {
                              string[] bird = new string[]
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
                              await e.Channel.SendMessage(bird[Global.random.Next(0, bird.Length)]);
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "cat";
                        x.ProcessCommand = async (e, args) =>
                        {
                            WebClient c = new WebClient();
                            byte[] b = c.DownloadData("http://random.cat/meow");
                            string str = Encoding.Default.GetString(b);
                            CatImage cat = JsonConvert.DeserializeObject<CatImage>(str);
                            await e.Channel.SendMessage(cat.File);
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "compliment";
                        x.ProcessCommand = async (e, args) =>
                        {
                            string[] I_LIKE = new string[]
                            {
                                "I like ",
                                "I love ",
                                "I admire ",
                                "I really enjoy ",
                                "For some reason i like "
                            };

                            string[] BODY_PART = new string[]
                            {
                                "the lower part of your lips",
                                "the smallest toe on your left foot",
                                "the smallest toe on your right foot",
                                "the second eyelash from your left eye",
                                "the lower part of your chin",
                                "your creepy finger in your left hand",
                                "your cute smile",
                                "those dazzling eyes of yours",
                                "your creepy finger in your right hand",
                                "the special angles your elbows makes",
                                "the dimples on your cheeks",
                                "your smooth hair"
                            };

                            string[] SUFFIX = new string[]
                            {
                                " alot",
                                " a bit",
                                " quite a bit",
                                " a lot, is that weird?",
                                ""
                            };

                            await e.Channel.SendMessage(I_LIKE[Global.random.Next(0, I_LIKE.Length)] + BODY_PART[Global.random.Next(0, BODY_PART.Length)] + SUFFIX[Global.random.Next(0, SUFFIX.Length)]);
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "cage";
                        x.ProcessCommand = async (e, args) =>
                        {
                            await e.Channel.SendMessage("http://www.placecage.com/c/" + Global.random.Next(100, 1500) + "/" + Global.random.Next(100, 1500));
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "ctb";
                        x.GuildPermissions = new List<DiscordGuildPermission> { DiscordGuildPermission.AttachFiles };
                        x.ProcessCommand = async (e, args) =>
                        {
                            using (WebClient webClient = new WebClient())
                            {
                                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + args + "&mode=2&countryrank");

                                using (MemoryStream mem = new MemoryStream(data))
                                {
                                    await e.Channel.SendFileAsync(mem, $"png");
                                }
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "dog";
                        x.ProcessCommand = async (e, args) =>
                        {
                            string[] dog = new string[]
                            {
                                "http://i.imgur.com/KOjUbMQ.jpg",
                                "http://i.imgur.com/owJKr7y.jpg",
                                "http://i.imgur.com/rpQdRoY.jpg",
                                "http://i.imgur.com/xrVXDQ2.jpg",
                                "http://i.imgur.com/Bt6zrhq.jpg",
                                "http://i.imgur.com/sNm8hRR.jpg",
                                "http://i.imgur.com/TeTwhxN.jpg",
                                "http://i.imgur.com/zMs8Sx6.jpg",
                                "http://i.imgur.com/bWioilW.jpg",
                                "http://i.imgur.com/q6UTI0W.jpg",
                                "http://i.imgur.com/e9aTxh7.jpg",
                                "http://i.imgur.com/lvH26a9.jpg",
                                "http://i.imgur.com/0Q8E82b.jpg",
                                "http://i.imgur.com/oMm6Zba.jpg",
                                "http://i.imgur.com/aA4kvJE.jpg",
                                "http://i.imgur.com/FJRTLZR.jpg",
                                "http://i.imgur.com/EHYhgJk.jpg",
                                "http://i.imgur.com/QiudnCT.jpg",
                                "http://i.imgur.com/2nYIwTd.jpg",
                                "http://i.imgur.com/NnFZVPC.jpg",
                                "http://i.imgur.com/uEHtSpB.jpg",
                                "http://i.imgur.com/5DPqIi0.jpg",
                                "http://i.imgur.com/ZkxnaRE.jpg",
                                "http://i.imgur.com/kCWMUgk.jpg",
                                "http://i.imgur.com/X8FO7Ds.jpg",
                                "http://i.imgur.com/yKAXiyl.jpg",
                                "http://i.imgur.com/A4eVQQF.jpg",
                                "http://i.imgur.com/Wtjxxiv.jpg",
                                "http://i.imgur.com/uIX5RVf.jpg",
                                "http://i.imgur.com/49Da81l.jpg"
                            };
                            await e.Channel.SendMessage(dog[Global.random.Next(0, dog.Length)]);
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "gif";
                        x.ProcessCommand = async(e, args) =>
                        {
                            if (string.IsNullOrEmpty(args)) return;

                            var client = new MashapeClient(Global.ImgurClientId, Global.ImgurKey);
                            var endpoint = new GalleryEndpoint(client);
                            var images = await endpoint.SearchGalleryAsync($"title:{args} ext:gif");
                            List<IGalleryImage> actualImages = new List<IGalleryImage>();
                            foreach(IGalleryItem item in images)
                            {
                                if(item as IGalleryImage != null)
                                {
                                    actualImages.Add(item as IGalleryImage);
                                }
                            }

                            if(actualImages.Count > 0)
                            {
                                IGalleryImage i = actualImages[Global.random.Next(0, actualImages.Count)];

                                await e.Channel.SendMessage(i.Link);
                            }
                            else
                            {
                                await e.Channel.SendMessage(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(Locale.ImageNotFound));
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "img";
                        x.ProcessCommand = async (e, args) =>
                        {
                            if (string.IsNullOrEmpty(args)) return;

                            var client = new MashapeClient(Global.ImgurClientId, Global.ImgurKey);
                            var endpoint = new GalleryEndpoint(client);
                            var images = await endpoint.SearchGalleryAsync($"title:{args}");
                            List<IGalleryImage> actualImages = new List<IGalleryImage>();
                            foreach(IGalleryItem item in images)
                            {
                                if(item as IGalleryImage != null)
                                {
                                    actualImages.Add(item as IGalleryImage);
                                }
                            }

                            if(actualImages.Count > 0)
                            {
                                IGalleryImage i = actualImages[Global.random.Next(0, actualImages.Count)];

                                await e.Channel.SendMessage(i.Link);
                            }
                            else
                            {
                                await e.Channel.SendMessage(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(Locale.ImageNotFound));
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "lewd";
                        x.ProcessCommand = async (e, args) =>
                        {
                            string[] lewd = new string[]
                            {
                                "http://i.imgur.com/eG42EVs.png",
                                "http://i.imgur.com/8shK3jh.png",
                                "http://i.imgur.com/uLKC84x.jpg",
                                "http://i.imgur.com/PZCwyyE.png",
                                "http://i.imgur.com/KWklw30.png",
                                "http://i.imgur.com/aoLsNgx.jpg",
                                "http://i.imgur.com/wyJAMVt.jpg",
                                "http://i.imgur.com/2Y5ZgHH.png",
                                "http://i.imgur.com/OIZyqxL.jpg",
                                "http://i.imgur.com/cejd1c0.gif",
                                "http://i.imgur.com/Obl7JvE.png",
                                "http://i.imgur.com/PFFmM1q.png",
                                "http://i.imgur.com/2vopeCM.jpg",
                                "http://i.imgur.com/U4Nk0e5.jpg",
                                "http://i.imgur.com/Llf61b1.jpg",
                                "http://i.imgur.com/3vYPbuO.jpg",
                                "http://i.imgur.com/p1twVD4.png",
                                "http://i.imgur.com/AsxaQ3D.gif",
                                "http://i.imgur.com/On8Axls.gif"
                            };
                            await e.Channel.SendMessage(lewd[Global.random.Next(0, lewd.Length)]);
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "mania";
                        x.GuildPermissions = new List<DiscordGuildPermission> { DiscordGuildPermission.AttachFiles };
                        x.ProcessCommand = async (e, args) =>
                        {
                            using (WebClient webClient = new WebClient())
                            {
                                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + args + "&mode=3&countryrank");

                                using (MemoryStream mem = new MemoryStream(data))
                                {
                                    await e.Channel.SendFileAsync(mem, $"png");
                                }
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "osu";
                        x.GuildPermissions = new List<DiscordGuildPermission> { DiscordGuildPermission.AttachFiles };
                        x.ProcessCommand = async (e, args) =>
                        {
                            using (WebClient webClient = new WebClient())
                            {
                                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + args + "&countryrank"); 

                                using (MemoryStream mem = new MemoryStream(data))
                                {
                                    await e.Channel.SendFileAsync(mem, $"png");
                                }
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "pick";
                      x.ProcessCommand = async (e, args) =>
                      {
                          if (string.IsNullOrWhiteSpace(args))
                          {
                              await e.Channel.SendMessage(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(Locale.ErrorPickNoArgs));
                              return;
                          }
                          string[] choices = args.Split(',');

                          Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());
                          await e.Channel.SendMessage(locale.GetString(Locale.PickMessage, new object[]{ e.Author.Username, choices[Global.random.Next(0, choices.Length)] }));
                      };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "pun";
                        x.ProcessCommand = async (e, args) =>
                        {
                            await e.Channel.SendMessage(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(puns[Global.random.Next(0, puns.Length)]));
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "roll";
                        x.ProcessCommand = async (e, args) =>
                        {
                            string rollCalc = "";
                            string amount = "";
                            int rollAmount = 0;

                            if (args != "")
                            {
                                amount = args.Split(' ')[0];

                                if (amount.Split('d').Length > 1)
                                {
                                    for (int i = 0; i < int.Parse(amount.Split('d')[0]); i++)
                                    {
                                        int num = Mathm.Roll(int.Parse(amount.Split('d')[1]), 0);
                                        rollAmount += num;
                                        rollCalc += num + " + ";
                                    }
                                    rollCalc = rollCalc.Remove(rollCalc.Length - 3);
                                }
                                else
                                {
                                    try
                                    {
                                        rollAmount = Mathm.Roll(int.Parse(amount), 0);
                                    }
                                    catch
                                    {
                                        rollAmount = Mathm.Roll();

                                    }
                                }
                            }
                            else
                            {
                                rollAmount = Mathm.Roll();
                            }

                            if(rollAmount == 1)
                            {
                                await AchievementManager.Instance.GetContainerById("badluck").CheckAsync(new Accounts.Achievements.Objects.BasePacket(){ discordUser = e.Author, discordChannel = e.Channel });
                            }

                            
                            await e.Channel.SendMessage(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(Locale.RollResult, new object[]{ e.Author.Username, rollAmount }) + (rollCalc != "" ? " (" + rollCalc + ")" : ""));
                        };
                    }),
                    new CommandEvent(x =>
                    {
                           x.Name = "roulette";
                           x.ProcessCommand = async (e, args) =>
                           {
                               IEnumerable<IDiscordUser> users = await e.Channel.GetUsersAsync();

                               Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

                               if (e.Content.Split(' ').Length == 1)
                               {
                                   await e.Channel.SendMessage(locale.GetString(Locale.RouletteMessageNoArg, new object[]{ "<@" + users.ElementAt(Global.random.Next(0, users.Count())).Id + ">" }));
                               }
                               else
                               {
                                   await e.Channel.SendMessage(locale.GetString(Locale.RouletteMessage, new object[]{ args,  "<@" + users.ElementAt(Global.random.Next(0, users.Count())).Id + ">" }));
                               }
                           };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "safe";
                        x.ProcessCommand = async (e, args) =>
                        {
                            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

                            IPost s = null;
                            if (args.ToLower().StartsWith("use"))
                            {
                                string[] a = args.Split(' ');
                                args = args.Substring(a[0].Length);
                                switch (a[0].Split(':')[1].ToLower())
                                {
                                    case "safebooru":
                                    {
                                        s = SafebooruPost.Create(args, ImageRating.SAFE);
                                    } break;
                                    case "gelbooru":
                                    {
                                        s = GelbooruPost.Create(args, ImageRating.SAFE);
                                    } break;
                                    case "konachan":
                                    {
                                        s = KonachanPost.Create(args, ImageRating.SAFE);
                                    } break;
                                    case "e621":
                                    {
                                        s = E621Post.Create(args, ImageRating.QUESTIONABLE);
                                    } break;
                                    default:
                                    {
                                        await e.Channel.SendMessage("I do not support this image host :(");
                                    } break;
                                }
                            }
                            else
                            {
                                s = SafebooruPost.Create(args, ImageRating.SAFE);
                            }

                        if(s == null)
                        {
                            await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "We couldn't find an image with these tags!"));
                            return;
                        }

                        await e.Channel.SendMessage(s.ImageUrl);
                    };
                }),
                    new CommandEvent(x =>
                    {
                        x.Name = "taiko";
                        x.GuildPermissions = new List<DiscordGuildPermission> { DiscordGuildPermission.AttachFiles };
                        x.ProcessCommand = async (e, args) =>
                        {
                            using (WebClient webClient = new WebClient())
                            {
                                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + args + "&mode=1&countryrank");

                                using (MemoryStream mem = new MemoryStream(data))
                                {
                                    await e.Channel.SendFileAsync(mem, $"png");
                                }
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.Name = "slots";
                        x.ProcessCommand = async (e, args) =>
                        {
                            int moneyBet = 0;

                            using(var context = new MikiContext())
                            {
                                User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                                Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

                                if(!string.IsNullOrWhiteSpace(args))
                                {
                                    moneyBet = int.Parse(args);

                                    if(moneyBet > u.Currency)
                                    {
                                        await e.Channel.SendMessage(locale.GetString(Locale.InsufficientMekos));
                                        return;
                                    }
                                }

                                int moneyReturned = 0;

                                if(moneyBet <= 0)
                                {
                                    return;
                                }

                                string[] objects =
                                {
                                    "🍒", "🍒", "🍒", "🍒",
                                    "🍊", "🍊",
                                    "🍓", "🍓",
                                    "🍍","🍍",
                                    "🍇", "🍇",
                                    "⭐", "⭐",
                                    "🍍", "🍍",
                                    "🍓", "🍓",
                                    "🍊", "🍊", "🍊",
                                    "🍒", "🍒", "🍒", "🍒",
                                };

                                EmbedBuilder b = new EmbedBuilder();
                                b.Title = locale.GetString(Locale.SlotsHeader);

                                Random r = new Random();

                                string[] objectsChosen =
                                {
                                    objects[r.Next(objects.Length)],
                                    objects[r.Next(objects.Length)],
                                    objects[r.Next(objects.Length)]
                                };

                                Dictionary<string, int> score = new Dictionary<string, int>();

                                foreach(string o in objectsChosen)
                                {
                                    if(score.ContainsKey(o))
                                    {
                                        score[o]++;
                                        continue;
                                    }
                                    score.Add(o, 1);
                                }

                                if(score.ContainsKey("🍒"))
                                {
                                    if(score["🍒"] == 2)
                                    {
                                        moneyReturned = (int)Math.Ceiling(moneyBet * 0.5f);
                                    }
                                    else if(score["🍒"] == 3)
                                    {
                                        moneyReturned = (int)Math.Ceiling(moneyBet * 1f);
                                    }
                                }
                                if(score.ContainsKey("🍊"))
                                {
                                    if(score["🍊"] == 2)
                                    {
                                        moneyReturned = (int)Math.Ceiling(moneyBet * 0.8f);
                                    }
                                    else if(score["🍊"] == 3)
                                    {
                                        moneyReturned = (int)Math.Ceiling(moneyBet * 1.5f);
                                    }
                                }
                                if(score.ContainsKey("🍓"))
                                {
                                    if(score["🍓"] == 2)
                                    {
                                        moneyReturned = (int)Math.Ceiling(moneyBet * 1f);
                                    }
                                    else if(score["🍓"] == 3)
                                    {
                                        moneyReturned = (int)Math.Ceiling(moneyBet * 2f);
                                    }
                                }
                                if(score.ContainsKey("🍍"))
                                {
                                    if(score["🍍"] == 3)
                                    {
                                        moneyReturned = (int)Math.Ceiling(moneyBet * 4f);
                                    }
                                }
                                if(score.ContainsKey("🍇"))
                                {
                                    if(score["🍇"] == 3)
                                    {
                                        moneyReturned = (int)Math.Ceiling(moneyBet * 6f);
                                    }
                                }
                                if(score.ContainsKey("⭐"))
                                {
                                    if(score["⭐"] == 3)
                                    {
                                        moneyReturned = (int)Math.Ceiling(moneyBet * 12f);
                                    }
                                }

                                if(moneyReturned == 0)
                                {
                                    moneyReturned = -moneyBet;
                                }
                                else
                                {
                                    b.AddField(f =>
                                    {
                                        f.Name = locale.GetString(Locale.SlotsWinHeader);
                                        f.Value = locale.GetString(Locale.SlotsWinMessage, moneyReturned);
                                    });
                                }

                                b.Description = string.Join(" ", objectsChosen);
                                u.Currency += moneyReturned;
                                await context.SaveChangesAsync();
                                await e.Channel.SendMessage(new RuntimeEmbed(b));
                            }
                        };
                    }),
                };
            });

            await new RuntimeModule(module_fun)
                .AddCommand(new RuntimeCommandEvent("remind")
                    .Default(DoRemind))
                .InstallAsync(bot);
        }

        // >remind do the dishes in 120 seconds
        public async Task DoRemind(IDiscordMessage msg, string args)
        {
            List<string> arguments = args.Split(' ').ToList();
            int splitIndex = 0;

            for(int i = 0; i < arguments.Count; i++)
            {
                if(arguments[i].ToLower() == "in")
                {
                    splitIndex = i;
                }
            }

            if(splitIndex == 0)
            {
                // throw error
                return;
            }

            TimeSpan timeUntilReminder = new TimeSpan();
            string reminderText;

            List<string> timeList = new List<string>();
            timeList.AddRange(arguments);
            timeList.RemoveRange(0, splitIndex);

            int count = arguments.Count;
            arguments.RemoveRange(splitIndex, count - (splitIndex + 1));
            reminderText = string.Join(" ", arguments);

            if (reminderText.StartsWith("me to "))
            {
                reminderText = reminderText.Substring(6);
            }

            for (int i = 1; i < timeList.Count; i++)
            {
                switch(timeList[i])
                {
                    case "seconds":
                    case "second":
                        int seconds = int.Parse(timeList[i - 1]);
                        timeUntilReminder.Add(new TimeSpan(0, 0, seconds));
                        break;
                    case "minutes":
                    case "minute":
                        int minutes = int.Parse(timeList[i - 1]);
                        timeUntilReminder.Add(new TimeSpan(0, minutes, 0));
                        break;
                    case "hours":
                    case "hour":
                        int hours = int.Parse(timeList[i - 1]);
                        timeUntilReminder.Add(new TimeSpan(hours, 0 , 0));
                        break;
                    case "days":
                    case "day":
                        int days = int.Parse(timeList[i - 1]);
                        timeUntilReminder.Add(new TimeSpan(days, 0, 0, 0));
                        break;
                }
            }

            await Utils.Embed
                .SetTitle("👌 OK")
                .SetDescription($"I'll remind you to {reminderText} in {timeUntilReminder.ToTimeString()}")
                .SetColor(Meru.SDK.Color.GetColor(IAColor.GREEN))
                .SendToChannel(msg.Channel.Id);
        }
    }
}