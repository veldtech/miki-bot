using IA;
using IA.Events;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.API.RocketLeague;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    class RocketLeagueModule
    {
        RocketLeagueApi api = new RocketLeagueApi(new RocketLeagueOptions()
        {
            ApiKey = Global.RocketLeagueKey
        });

        public async Task LoadEvent(Bot bot)
        {
            await new RuntimeModule("Rocket League")
                .AddCommand(new RuntimeCommandEvent("rluser")
                    .Default(GetUser))
                .AddCommand(new RuntimeCommandEvent("rlsearchuser")
                    .Default(SearchUser))
                .AddCommand(new RuntimeCommandEvent("rluserseason")
                    .Default(GetUserSeason))
                .AddCommand(new RuntimeCommandEvent("rlnowplaying")
                    .Default(GetNowPlaying))
                .InstallAsync(bot);
        }

        public async Task GetUser(EventContext e)
        {
            string[] arg = e.arguments.Split(' ');
            int platform = 1;

            if (arg.Length > 1)
            {
                platform = GetPlatform(arg[1]);
            }

            IDiscordEmbed embed = Utils.Embed;

            RocketLeagueUser user = await TryGetUser(arg[0], platform);

            if (user == null)
            {
                embed.Title = "Uh oh!";
                embed.Description = $"We couldn't find a user with the name `{arg[0]}`. Please look up yourself on https://rlstats.com/ to create your profile!";
                embed.ThumbnailUrl = "http://miki.veld.one/assets/img/rlstats-logo.png";
                await e.Channel.SendMessage(embed);
                return;
            }

            embed.Title = user.DisplayName;

            foreach (RocketLeagueSeason season in api.seasons.Data)
            {
                if (user.RankedSeasons.ContainsKey(season.Id))
                {
                    Dictionary<int, RocketLeagueRankedStats> rankedseason = user.RankedSeasons[season.Id];
                    string s = "";

                    foreach (RocketLeaguePlaylist playlist in api.playlists.Data)
                    {
                        if (rankedseason.ContainsKey(playlist.Id))
                        {
                            if (playlist.PlatformId == platform)
                            {
                                RocketLeagueRankedStats stats = rankedseason[playlist.Id];
                                s += "`" + playlist.Name.Substring(7).PadRight(13) + ":` " + stats.RankPoints.ToString() + " MMR\n";
                            }
                        }
                    }

                    embed.AddField(f => { f.Name = "Season" + season.Id; f.Value = s; f.IsInline = true; });
                }
            }

            embed.ThumbnailUrl = user.AvatarUrl;
            embed.ImageUrl = user.SignatureUrl;
            await e.Channel.SendMessage(embed);
        }
        public async Task GetUserSeason(EventContext e)
        {
            int platform = 1;
            string[] arg = e.arguments.Split(' ');
            int seasonId = int.Parse(arg[1]);

            if (arg.Length > 2)
            {
                platform = GetPlatform(arg[2]);
            }

            IDiscordEmbed embed = Utils.Embed;
            RocketLeagueUser user = await TryGetUser(arg[0], platform);

            if (user == null)
            {
                embed.Title = "Uh oh!";
                embed.Description = $"We couldn't find a user with the name `{arg[0]}`. Please look up yourself on https://rlstats.com/ to create your profile!";
                embed.ThumbnailUrl = "http://miki.veld.one/assets/img/rlstats-logo.png";
                await e.Channel.SendMessage(embed);
                return;
            }

            embed.Title = $"{user.DisplayName}'s Season {arg[1]}";

            if (user.RankedSeasons.ContainsKey(seasonId))
            {
                Dictionary<int, RocketLeagueRankedStats> rankedseason = user.RankedSeasons[seasonId];

                foreach (RocketLeaguePlaylist playlist in api.playlists.Data)
                {
                    if (rankedseason.ContainsKey(playlist.Id))
                    {
                        if (playlist.PlatformId == platform)
                        {
                            RocketLeagueRankedStats stats = rankedseason[playlist.Id];

                            string rank = "";
                            RocketLeagueTier t = api.tiers.Data.Find(z => { return z.Id == stats.Tier; });

                            if (t != null)
                            {
                                rank = t.Name + " " + stats.Division;

                                embed.AddField(f =>
                                {
                                    f.Name = playlist.Name.Substring(7);
                                    f.Value = $"Rank: {rank}\n\nMMR: {stats.RankPoints}\nMatches Played: {stats.MatchesPlayed}";
                                    f.IsInline = true;
                                });
                            }
                        }
                    }
                }
            }

            await e.Channel.SendMessage(embed);
        }

        public async Task GetNowPlaying(EventContext e)
        {
            int platform = -1;

            if (!string.IsNullOrWhiteSpace(e.arguments))
            {
                platform = GetPlatform(e.arguments);
            }

            Dictionary<int, RocketLeaguePlaylist> d = new Dictionary<int, RocketLeaguePlaylist>();

            if (platform == -1)
            {
                foreach (RocketLeaguePlaylist p in api.playlists.Data)
                {
                    if (d.ContainsKey(p.Id))
                    {
                        d[p.Id].Population.Players += p.Population.Players;
                    }
                    else
                    {
                        RocketLeaguePlaylist playlist = new RocketLeaguePlaylist();
                        playlist.Id = p.Id;
                        playlist.Name = p.Name;
                        playlist.PlatformId = p.PlatformId;
                        playlist.Population = new RocketLeaguePopulation();
                        playlist.Population.Players = p.Population.Players;

                        d.Add(p.Id, playlist);
                    }
                }
            }
            else
            {
                foreach (RocketLeaguePlaylist p in api.playlists.Data)
                {
                    if (p.PlatformId == platform)
                    {
                        d.Add(p.Id, p);
                    }
                }
            }

            IDiscordEmbed embed = Utils.Embed;
            embed.Title = "Now Playing!";
            foreach (RocketLeaguePlaylist p in d.Values)
            {
                embed.AddField(f =>
                {
                    f.Name = api.playlists.Data.Find(z => { return z.Id == p.Id; }).Name;
                    f.Value = p.Population.Players.ToString();
                    f.IsInline = true;
                });
            }

            await e.Channel.SendMessage(embed);
        }

        public async Task SearchUser(EventContext e)
        {
            string[] arg = e.arguments.Split(' ');
            IDiscordEmbed embed = Utils.Embed;
            RocketLeagueSearchResult user = await api.SearchUsersAsync(arg[0], (arg.Length >= 2) ? int.Parse(arg[1]) : 0);

            embed.Title = $"Found {user.TotalResults} users with the name `{arg[0]}`";
            embed.CreateFooter();
            embed.Footer.Text = $"Page {user.Page} of ${(int)Math.Ceiling((double)user.TotalResults / user.MaxResultsPerPage)}";

            List<string> names = new List<string>();

            user.Data.ForEach(z => { names.Add(z.DisplayName); });

            embed.Description = string.Join(", ", names);

            await e.Channel.SendMessage(embed);
        }

        public async Task<RocketLeagueUser> TryGetUser(string name, int platform)
        {
            RocketLeagueUser user = await api.GetUserAsync(name, platform);

            if (user.DisplayName == null || user.DisplayName == "")
            {
                RocketLeagueSearchResult result = await api.SearchUsersAsync(name, 0, true);
                if (result.Data.Count > 0)
                {
                    user = result.Data[0];
                }
                else
                {
                    return null;
                }
            }

            return user;
        }

        public int GetPlatform(string platformName)
        {
            switch (platformName.ToLower())
            {
                case "steam":
                case "pc":
                    return 1;
                case "ps4":
                case "playstation":
                case "ps":
                    return 2;
                case "xbox":
                case "xbone":
                case "xboxone":
                    return 3;
            }
            return 1;
        }
    }
}
