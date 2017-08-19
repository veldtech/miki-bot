using IA;
using IA.Events.Attributes;
using IA.Extension;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using Miki.Accounts.Achievements;
using Miki.Accounts.Achievements.Objects;
using Miki.API;
using Miki.Languages;
using Miki.Models;
using Miki.Objects;
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

namespace Miki.Modules.OsuModule
{
    [Module("Osu")]
    public class OsuModule
    {
        public OsuModule(RuntimeModule module)

        [Command(Name = "osu")]
        public async Task SendOsuSignatureAsync(EventContext e)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&countryrank");

                using (MemoryStream mem = new MemoryStream(data))
                {
                    await e.Channel.SendFileAsync(mem, $"sig.png");
                }
            }
        }

        [Command(Name = "ctb")]
        public async Task SendCatchTheBeatSignatureAsync(EventContext e)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&mode=2&countryrank");

                using (MemoryStream mem = new MemoryStream(data))
                {
                    await e.Channel.SendFileAsync(mem, $"{e.arguments}.png");
                }
            }
        }

        [Command(Name = "mania")]
        public async Task SendManiaSignatureAsync(EventContext e)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&mode=3&countryrank");

                using (MemoryStream mem = new MemoryStream(data))
                {
                    await e.Channel.SendFileAsync(mem, $"sig.png");
                }
            }
        }

        [Command(Name = "taiko")]
        public async Task SendTaikoSignatureAsync(EventContext e)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&mode=1&countryrank");

                using (MemoryStream mem = new MemoryStream(data))
                {
                    await e.Channel.SendFileAsync(mem, $"sig.png");
                }
            }
        }
    }
}
