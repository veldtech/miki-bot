namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FKBadgedOwned : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BadgeResources",
                c => new
                    {
                        id = c.Long(nullable: false),
                        creator_id = c.Long(nullable: false),
                        date_added = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.Users", t => t.creator_id, cascadeDelete: true)
                .Index(t => t.creator_id);
            
            AddColumn("dbo.BadgesOwned", "Resource_Id", c => c.Long());
            CreateIndex("dbo.BadgesOwned", "Resource_Id");
            AddForeignKey("dbo.BadgesOwned", "Resource_Id", "dbo.BadgeResources", "id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.BadgeResources", "creator_id", "dbo.Users");
            DropForeignKey("dbo.BadgesOwned", "Resource_Id", "dbo.BadgeResources");
            DropIndex("dbo.BadgesOwned", new[] { "Resource_Id" });
            DropIndex("dbo.BadgeResources", new[] { "creator_id" });
            DropColumn("dbo.BadgesOwned", "Resource_Id");
            DropTable("dbo.BadgeResources");
        }
    }
}
