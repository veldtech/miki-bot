using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Miki.Models
{
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
        [Column("SettingId", Order = 1)]
        public DatabaseSettingId SettingId { get; set; }

        [Column("IsEnabled")]
        [DefaultValue("true")]
        public bool IsEnabled { get; set; }
    }
}