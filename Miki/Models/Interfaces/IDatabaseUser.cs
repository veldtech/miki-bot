using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models.Interfaces
{
    public interface IDatabaseUser : IDatabaseEntity
    {
        string Name { get; set; }
    }
}
