using IA;
using IA.SDK;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        [Column("RequiredLevel")]
        public int RequiredLevel { get; set; }

        [NotMapped]
        public IDiscordRole Role => new RuntimeRole(Bot.instance.Client.GetGuild(GuildId.FromDbLong()).GetRole(RoleId.FromDbLong()));
    }
}
