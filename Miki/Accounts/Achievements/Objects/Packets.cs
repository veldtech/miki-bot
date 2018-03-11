using Discord;
using Miki.Common;
using Miki.Framework.Events;
using Miki.Models;

namespace Miki.Accounts.Achievements.Objects
{
    public class BasePacket
    {
        public IUser discordUser;
        public IMessageChannel discordChannel;
    }

    public class AchievementPacket : BasePacket
    {
        public BaseAchievement achievement;
        public int count;
    }

    public class MessageEventPacket : BasePacket
    {
        public IMessage message;
    }

    public class UserUpdatePacket : BasePacket
    {
        public IUser userNew;
    }

    public class TransactionPacket : BasePacket
    {
        public User receiver;
        public User giver;
        public int amount;
    }

    public class CommandPacket : BasePacket
    {
        public IMessage message;
        public CommandEvent command;
        public bool success;
    }

    public class LevelPacket : BasePacket
    {
        public User account;
        public int level;
    }
}