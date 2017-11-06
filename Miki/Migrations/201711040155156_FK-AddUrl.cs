namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FKAddUrl : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BadgeResources", "url", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.BadgeResources", "url");
        }
    }
}
