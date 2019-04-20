using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Api.Models
{
    public class UserInventory
    {
        public List<UserItem> Items { get; set; }
    }

    public class UserItem
    {
        public int Id { get; set; }
        
        public int Amount { get; set; }
    }
}