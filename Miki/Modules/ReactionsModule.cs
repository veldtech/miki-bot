using IA;
using IA.Events;
using IA.SDK;
using IA.SDK.Events;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.Modules
{
    internal class ReactionsModule
    {
        public async Task LoadEvents(Bot bot)
        {
            IModule m = new Module(mod =>
            {
                mod.Name = "Reactions";
                mod.Events = new List<ICommandEvent>()
                {
                    // Confused
                    new CommandEvent(x =>
                    {
                        x.Name = "confused";
                        x.Metadata = new EventMetadata(
                            "Consfuse your opponents with the power of cuties! <3",
                            "Couldn't interrogate suspect!! '0'?!",
                            ">confused");
                        x.ProcessCommand = async (e, args) =>
                        {
                            string[] images = new string[]
                            {
                                "http://i.imgur.com/RCotXAK.png",
                                "http://i.imgur.com/yN5cwQq.jpg",
                                "http://i.imgur.com/5TkmRWv.png",
                                "http://i.imgur.com/QBFQzCQ.png",
                                "http://i.imgur.com/KjSp1W4.png",
                                "http://i.imgur.com/mX6D68m.jpg",
                                "http://i.imgur.com/ogA5GeN.jpg",
                                "http://i.imgur.com/eCHsHQZ.jpg",
                                "http://i.imgur.com/r0u2dBx.jpg",
                                "http://i.imgur.com/d8oMJUg.jpg",
                                "http://i.imgur.com/dkV331J.jpg",
                                "http://i.imgur.com/U9N4oGR.jpg",
                                "http://i.imgur.com/GA0ZtvR.jpg",
                                "http://i.imgur.com/NQ2e3Dq.gifv",
                                "http://i.imgur.com/5HTugJ6.jpg",
                                "http://i.imgur.com/MJrBLij.png",
                                "http://i.imgur.com/JgjCHPd.jpg",
                                "http://i.imgur.com/KIDXXHw.gifv",
                                "http://i.imgur.com/Eu0Yyqq.jpg",
                                "http://i.imgur.com/P5V369I.png",
                                "http://i.imgur.com/DtVEGde.gifv",
                                "http://i.imgur.com/xxNH850.jpg",
                                "http://i.imgur.com/gytATzW.jpg",
                                "http://i.imgur.com/UrDJVC0.jpg",
                                "http://i.imgur.com/3GkAnYo.png",
                                "http://i.imgur.com/qTXPgyI.jpg",
                                "http://i.imgur.com/GmIXuso.png",
                                "http://i.imgur.com/UM8XpgR.gif",
                                "http://i.imgur.com/GhoKM0u.gif",
                            };

                            RuntimeEmbed em = new RuntimeEmbed(new Discord.EmbedBuilder());

                            em.ImageUrl = images[Global.random.Next(0, images.Length)];

                            await e.Channel.SendMessage(em);
                        };
                    }),

                    // Pout
                    new CommandEvent(x =>
                    {
                        x.Name = "pout";

                        x.Metadata = new EventMetadata(
                            "NO, im not HAPPY! baka!",
                            "NO, no pouts for you!!! baka",
                            ">pout");

                        x.ProcessCommand = async (e, args) =>
                        {
                            string[] images = new string[]
                            {
                                "http://i.imgur.com/hsjBcz1.jpg",
                                "http://i.imgur.com/oJSVNzT.jpg",
                                "http://i.imgur.com/gWtmHoN.jpg",
                                "http://i.imgur.com/VFG9zKV.png",
                                "http://i.imgur.com/BUBiL0f.jpg",
                                "http://i.imgur.com/UdlZ69E.gif",
                                "http://i.imgur.com/yhryAf9.png",
                                "http://i.imgur.com/d9DG2sJ.png",
                                "http://i.imgur.com/uTB2HIY.png",
                                "http://i.imgur.com/dVtR9vI.png",
                                "http://i.imgur.com/rt7Vgn3.jpg",
                                "http://i.imgur.com/uTB2HIY.png"
                            };

                            RuntimeEmbed em = new RuntimeEmbed(new Discord.EmbedBuilder());

                            em.ImageUrl = images[Global.random.Next(0, images.Length)];

                            await e.Channel.SendMessage(em);
                        };
                    }),

                    // Smug
                    new CommandEvent(x =>
                    {
                    x.Name = "smug";
                    x.ProcessCommand = async (e, args) =>
                    {
                        string[] images = new string[]
                        {
                            "http://i.imgur.com/zUwqrhM.png",
                            "http://i.imgur.com/TYqPh89.jpg",
                            "http://i.imgur.com/xyOSaCt.png",
                            "http://i.imgur.com/gyw0ifl.png",
                            "http://i.imgur.com/kk0xvtx.png",
                            "http://i.imgur.com/UIuyUne.jpg",
                            "http://i.imgur.com/9zgIjY1.jpg",
                            "http://i.imgur.com/Ku1ONAD.jpg",
                            "http://i.imgur.com/7lB5bRT.jpg",
                            "http://i.imgur.com/BoVHipF.jpg",
                            "http://i.imgur.com/vN48mwz.png",
                            "http://i.imgur.com/fGI4zLe.jpg",
                            "http://i.imgur.com/Gc4gmwQ.jpg",
                            "http://i.imgur.com/JMrmKt7.jpg",
                            "http://i.imgur.com/a7sbJz2.jpg",
                            "http://i.imgur.com/NebmjhR.png",
                            "http://i.imgur.com/5ccbrFI.png",
                            "http://i.imgur.com/XJL4Vmo.jpg",
                            "http://i.imgur.com/eg0q1ez.png",
                            "http://i.imgur.com/JJFxxmA.jpg",
                            "http://i.imgur.com/2cTDF3b.jpg",
                            "http://i.imgur.com/Xc0Duqv.png",
                            "http://i.imgur.com/YgMdPkd.jpg",
                            "http://i.imgur.com/BvAv6an.jpg",
                            "http://i.imgur.com/KRLP5JT.jpg",
                            "http://i.imgur.com/yXcsCK3.jpg",
                            "http://i.imgur.com/QXG56kG.png",
                            "http://i.imgur.com/OFBz1YJ.png",
                            "http://i.imgur.com/9ulVckY.png",
                            "http://i.imgur.com/VLXeSJK.png",
                            "http://i.imgur.com/baiMBP6.png"
                        };

                            RuntimeEmbed em = new RuntimeEmbed(new Discord.EmbedBuilder());

                            em.ImageUrl = images[Global.random.Next(0, images.Length)];

                            await e.Channel.SendMessage(em);
                    };
                }),
                };
            });
            await new RuntimeModule(m).InstallAsync(bot);
        }
    }
}