using Miki.Framework;
using Miki.Common;
using Miki.Common.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Miki.Models
{
    public class LevelRole
    {
        public long GuildId { get; set; }
        public long RoleId { get; set; }

        public int RequiredLevel { get; set; }
		public bool Automatic { get; set; }
		public bool Optable { get; set; }
		public long RequiredRole { get; set; }
		public int Price { get; set; }

        [NotMapped]
        public IDiscordRole Role => new RuntimeRole(Bot.instance.Client.GetGuild(GuildId.FromDbLong()).GetRole(RoleId.FromDbLong()));
    }
}