using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Framework.Commands.Permissions.Models
{
    public class Permission
    {
        public long UserId { get; set; }
        public long GuildId { get; set; }

        /// <summary>
        /// Long bit pool; max 64
        /// </summary>
        public int PermissionLevel { get; set; }
    }
}
