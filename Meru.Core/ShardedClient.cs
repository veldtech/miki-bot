using Discord;
using Discord.WebSocket;
using IA.SDK;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA
{
    // TODO: Either throw it away or keep it for later.
    public class ShardedClient
    {
        public event Func<int, Task> Ready;

        public event Func<IDiscordMessage, Task> MessageRecieved;

        private List<DiscordSocketClient> shards = new List<DiscordSocketClient>();

        private ClientInformation info = new ClientInformation();

        public List<DiscordSocketClient> Shards
        {
            get
            {
                return shards;
            }
        }

        public int ChannelCount
        {
            get
            {
                int channelCount = 0;
                foreach (DiscordSocketClient sc in shards)
                {
                    foreach (SocketGuild g in sc.Guilds)
                    {
                        channelCount += g.Channels.Count;
                    }
                }
                return channelCount;
            }
        }

        public int GuildCount
        {
            get
            {
                int guildCount = 0;
                foreach (DiscordSocketClient sc in shards)
                {
                    guildCount += sc.Guilds.Count;
                }
                return guildCount;
            }
        }

        public int UserCount
        {
            get
            {
                int channelCount = 0;
                foreach (DiscordSocketClient sc in shards)
                {
                    foreach (SocketGuild g in sc.Guilds)
                    {
                        channelCount += g.Users.Count;
                    }
                }
                return channelCount;
            }
        }

        public ShardedClient(ClientInformation clientInfo)
        {
            Log.Message(clientInfo.ShardCount.ToString());

            info = clientInfo;

            for (int i = 0; i < info.ShardCount; i++)
            {
                shards.Add(new DiscordSocketClient(new DiscordSocketConfig()
                {
                    ShardId = i,
                    TotalShards = info.ShardCount
                }));

                shards[i].MessageReceived += async (message) =>
                {
                    await OnMessageRecieved(shards[i], message);
                };
                shards[i].Ready += async () =>
                {
                    await OnReady(shards[i]);
                };
            }
        }

        public async Task ConnectAsync()
        {
            foreach (DiscordSocketClient client in shards)
            {
                Log.Message($"Connecting to shard {client.ShardId}");
                await client.LoginAsync(TokenType.Bot, info.Token);
                await client.StartAsync();
                await Task.Delay(6000);
            }
        }

        public void ForEachShard(Action<DiscordSocketClient> a)
        {
            foreach (DiscordSocketClient cli in shards)
            {
                a.Invoke(cli);
            }
        }

        public DiscordSocketClient GetShard(int id)
        {
            return shards[id];
        }

        public IDiscordUser GetUser(ulong id)
        {
            foreach (DiscordSocketClient client in shards)
            {
                IUser u = client.GetUser(id);

                if (u != null)
                {
                    return new RuntimeUser(u);
                }
            }
            return null;
        }

        private async Task OnMessageRecieved(DiscordSocketClient c, IMessage m)
        {
            IDiscordMessage msg = new RuntimeMessage(m);
            await MessageRecieved.Invoke(msg);
        }

        private async Task OnReady(DiscordSocketClient x)
        {
            await Ready.Invoke(x.ShardId);
        }
    }
}