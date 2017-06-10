using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
    public class BasePacket
    {
        public IDiscordUser discordUser;
        public IDiscordMessageChannel discordChannel;
    }
    public class AchievementPacket : BasePacket
    {
        public BaseAchievement achievement;
        public int count;
    }
    public class MessageEventPacket : BasePacket
    {
        public IDiscordMessage message;
    }
    public class UserUpdatePacket : BasePacket
    {
        public IDiscordUser userNew;
    }
    public class TransactionPacket : BasePacket
    {
        public User receiver;
        public User giver;
        public int amount;
    }
    public class CommandPacket : BasePacket
    {
        public IDiscordMessage message;
        public IEvent command;
        public bool success;
    }
    public class LevelPacket : BasePacket
    {
        public User account;
        public int level;
    }
}
