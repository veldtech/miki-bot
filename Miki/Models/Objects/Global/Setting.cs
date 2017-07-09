using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models
{
    public enum DatabaseEntityType
    {
        USER = 0,
        ROLE = 1,
        CHANNEL = 2,
        GUILD = 3
    }

    public enum DatabaseSettingId
    {
        PERSONALMESSAGE = 0,
        CHANNELMESSAGE = 1,
        ERRORMESSAGE = 2
    };

    [Table("Settings")]
    public class Setting
    {
        [Key]
        [Column("EntityId", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long EntityId { get; set; }

        [Key]
        [Column("EntityType", Order = 1)]
        public DatabaseEntityType EntityType { get; set; }

        [Key]
        [Column("SettingId", Order = 2)]
        public DatabaseSettingId SettingId { get; set; }

        [Column("IsEnabled")]
        [DefaultValue("true")]
        public bool IsEnabled { get; set; }
    }
}
