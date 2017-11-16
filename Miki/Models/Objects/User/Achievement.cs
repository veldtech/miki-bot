using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Miki.Models
{
    [Table("Achievements")]
    public class Achievement
    {
        [Key, Column("Id", Order = 0), DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [Key, Column("Name", Order = 1)]
        public string Name { get; set; }

        [Column("Rank")]
        public short Rank { get; set; }

		[Column("UnlockDate")]
		public DateTime UnlockDate { get; set; }
    }
}