namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addbans : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GuildUsers", "banned", c => c.Boolean(nullable: false));
            AddColumn("dbo.Users", "banned", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "banned");
            DropColumn("dbo.GuildUsers", "banned");
        }
    }
}
