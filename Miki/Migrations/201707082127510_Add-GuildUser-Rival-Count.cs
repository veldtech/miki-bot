namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGuildUserRivalCount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GuildUser", "RivalId", c => c.Long(nullable: false));
            AddColumn("dbo.GuildUser", "UserCount", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.GuildUser", "UserCount");
            DropColumn("dbo.GuildUser", "RivalId");
        }
    }
}
