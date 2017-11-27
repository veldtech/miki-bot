using IA;
using IA.SDK;
using IA.SDK.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Miki.Models
{
    [Table("LevelRoles")]
    public class LevelRole
    {
        [Key]
        [Column("GuildId", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long GuildId { get; set; }

        [Key]
        [Column("RoleId", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long RoleId { get; set; }

        [Column("RequiredLevel"), DefaultValue(0)]
        public int RequiredLevel { get; set; }

		[Column("Automatic"), DefaultValue(false)]
		public bool Automatic { get; set; }

		[Column("Optable"), DefaultValue(false)]
		public bool Optable { get; set; }

		[Column("RequiredRole"), DefaultValue(0)]
		public long RequiredRole { get; set; }

        [NotMapped]
        public IDiscordRole Role => new RuntimeRole(Bot.instance.Client.GetGuild(GuildId.FromDbLong()).GetRole(RoleId.FromDbLong()));
    }
}