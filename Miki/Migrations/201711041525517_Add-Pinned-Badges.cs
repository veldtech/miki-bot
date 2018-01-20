namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPinnedBadges : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PinnedBadges",
                c => new
                    {
                        id = c.Long(nullable: false, identity: true),
                        badges = c.String(),
                    })
                .PrimaryKey(t => t.id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.PinnedBadges");
        }
    }
}
