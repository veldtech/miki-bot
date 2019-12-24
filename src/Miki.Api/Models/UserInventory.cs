namespace Miki.Api.Models
{
    using System.Collections.Generic;

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