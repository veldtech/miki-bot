namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPinnedBadgesFK : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "PinnedBadges_Id", c => c.Long());
            CreateIndex("dbo.Users", "PinnedBadges_Id");
            AddForeignKey("dbo.Users", "PinnedBadges_Id", "dbo.PinnedBadges", "id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Users", "PinnedBadges_Id", "dbo.PinnedBadges");
            DropIndex("dbo.Users", new[] { "PinnedBadges_Id" });
            DropColumn("dbo.Users", "PinnedBadges_Id");
        }
    }
}
