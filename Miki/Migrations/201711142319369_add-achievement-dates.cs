namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addachievementdates : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Achievements", "UnlockDate", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Achievements", "UnlockDate");
        }
    }
}
