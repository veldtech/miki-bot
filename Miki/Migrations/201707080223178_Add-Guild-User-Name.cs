namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGuildUserName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GuildUser", "Name", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.GuildUser", "Name");
        }
    }
}
