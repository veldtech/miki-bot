namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FKAddName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BadgeResources", "name", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.BadgeResources", "name");
        }
    }
}
