using Discord;
using Discord.WebSocket;
using IA;
using IA.SDK;
using IA.SDK.Interfaces;
using Miki.Accounts.Achievements;
using Miki.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Miki
{
    internal class Notification
    {
        private const int SendNotificationId = 0;

        public static async Task SendPM(ulong userId, string message, DatabaseEntityType entityType = DatabaseEntityType.USER, DatabaseSettingId settingId = DatabaseSettingId.PERSONALMESSAGE)
        {
            IUser m = Bot.instance.Client.GetUser(userId);
            IDiscordUser user = new RuntimeUser(m);

            if (CanSendNotification(userId, entityType, settingId))
            {
                RuntimeEmbed e = new RuntimeEmbed(new Discord.EmbedBuilder());
                e.Title = "NOTIFICATION";
                e.Description = message;

                await user?.SendMessage(e);
                Log.Message("Sent notification to " + user.Username);
            }
        }
        public static async Task SendPM(ulong userId, IDiscordEmbed embed, DatabaseEntityType entityType = DatabaseEntityType.USER, DatabaseSettingId settingId = DatabaseSettingId.PERSONALMESSAGE)
        {
            IUser m = Bot.instance.Client.GetUser(userId);
            IDiscordUser user = new RuntimeUser(m);

            if (CanSendNotification(userId, entityType, settingId))
            {
                await user?.SendMessage(embed);
            }
        }
        public static async Task SendPM(IDiscordUser user, string message, DatabaseEntityType entityType = DatabaseEntityType.USER, DatabaseSettingId settingId = DatabaseSettingId.PERSONALMESSAGE)
        {
            if (CanSendNotification(user.Id, entityType, settingId))
            {
                RuntimeEmbed e = new RuntimeEmbed(new Discord.EmbedBuilder());
                e.Title = "NOTIFICATION";
                e.Description = message;

                await user.SendMessage(e);
            }
        }
        public static async Task SendPM(IDiscordUser user, IDiscordEmbed embed, DatabaseSettingId settingId = DatabaseSettingId.PERSONALMESSAGE)
        {
            if (CanSendNotification(user.Id, DatabaseEntityType.USER, settingId))
            {
                await user.SendMessage(embed);
            }
        }

        public static async Task SendChannel(IDiscordMessageChannel channel, string message)
        {
            if (CanSendNotification(channel.Guild.Id, DatabaseEntityType.GUILD, DatabaseSettingId.CHANNELMESSAGE))
            {
                await channel.SendMessage(message);
            }
        }
        public static async Task SendChannel(IDiscordMessageChannel channel, IDiscordEmbed message)
        {
            if (CanSendNotification(channel.Guild.Id, DatabaseEntityType.GUILD, DatabaseSettingId.CHANNELMESSAGE))
            {
                await channel.SendMessage(message);
            }
        }

        public static async Task SendAchievement(AchievementDataContainer<BaseAchievement> d, int rank, IDiscordMessageChannel channel, IDiscordUser user)
        { 
            await SendAchievement(d.Achievements[rank], channel, user);
        }
        public static async Task SendAchievement(BaseAchievement d, IDiscordMessageChannel channel, IDiscordUser user)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.Title = "ACHIEVEMENT UNLOCKED";
            embed.Description = $"{d.Icon} **{user.Username}** has unlocked the achievement **{d.Name}**! {d.Icon}";

            await SendChannel(channel, new RuntimeEmbed(embed));
        }

        public static bool CanSendNotification(ulong id, DatabaseEntityType entityType = DatabaseEntityType.USER, DatabaseSettingId settingId = DatabaseSettingId.PERSONALMESSAGE)
        {
            using (var context = new MikiContext())
            {
                Setting setting = context.Settings.Find(id.ToDbLong(), entityType, settingId);
                if (setting != null) return setting.IsEnabled;
                return true;
            }
        }
    }
}