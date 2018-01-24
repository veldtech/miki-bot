using Discord;
using IA;
using IA.SDK;
using IA.SDK.Interfaces;
using Miki.Accounts.Achievements;
using Miki.Models;
using System.Threading.Tasks;

namespace Miki
{
    internal class Notification
    {
        private const int SendNotificationId = 0;

        public static async Task SendPM(ulong userId, string message, DatabaseSettingId settingId = DatabaseSettingId.PERSONALMESSAGE)
        {
            IUser m = Bot.instance.Client.GetUser(userId);
            IDiscordUser user = new RuntimeUser(m);

            if (CanSendNotification(userId, settingId))
            {
                RuntimeEmbed e = new RuntimeEmbed(new Discord.EmbedBuilder());
                e.Title = "NOTIFICATION";
                e.Description = message;

				await e.QueueToUser(userId);
                Log.Message("Sent notification to " + user.Username);
            }
        }

        public static async Task SendPM(ulong userId, IDiscordEmbed embed, DatabaseSettingId settingId = DatabaseSettingId.PERSONALMESSAGE)
        {
            IUser m = Bot.instance.Client.GetUser(userId);
            IDiscordUser user = new RuntimeUser(m);

            if (CanSendNotification(userId, settingId))
            {
				await embed.QueueToUser(user);
            }
        }

        public static async Task SendPM(IDiscordUser user, string message, DatabaseSettingId settingId = DatabaseSettingId.PERSONALMESSAGE)
        {
            if (CanSendNotification(user.Id, settingId))
            {
                RuntimeEmbed e = new RuntimeEmbed(new Discord.EmbedBuilder());
                e.Title = "NOTIFICATION";
                e.Description = message;

				await e.QueueToUser(user);
			}
        }

        public static async Task SendPM(IDiscordUser user, IDiscordEmbed embed, DatabaseSettingId settingId = DatabaseSettingId.PERSONALMESSAGE)
        {
            if (CanSendNotification(user.Id, settingId))
            {
				await embed.QueueToUser(user);
            }
        }

        public static async Task SendChannel(IDiscordMessageChannel channel, string message)
        {
            if ((await channel.Guild.GetCurrentUserAsync()).HasPermissions(channel, DiscordGuildPermission.SendMessages))
            {
                if (CanSendNotification(channel.Guild.Id, DatabaseSettingId.CHANNELMESSAGE))
                {
                    await channel.QueueMessageAsync(message);
                }
            }
        }

        public static async Task SendChannel(IDiscordMessageChannel channel, IDiscordEmbed message)
        {
            if ((await channel.Guild.GetCurrentUserAsync()).HasPermissions(channel, DiscordGuildPermission.SendMessages))
            {
                if (CanSendNotification(channel.Guild.Id, DatabaseSettingId.CHANNELMESSAGE))
                {
                    await message.QueueToChannel(channel);
                }
            }
        }

        public static async Task SendAchievement(AchievementDataContainer d, int rank, IDiscordMessageChannel channel, IDiscordUser user)
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

        public static bool CanSendNotification(ulong id, DatabaseSettingId settingId = DatabaseSettingId.PERSONALMESSAGE)
        {
            bool output = true;
            Setting setting = null;

            using (var context = new MikiContext())
            {
                setting = context.Settings.Find(id.ToDbLong(), settingId);
                if (setting != null) output = setting.IsEnabled;
            }
            return output;
        }
    }
}