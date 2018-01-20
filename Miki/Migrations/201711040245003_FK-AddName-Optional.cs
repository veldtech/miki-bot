namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FKAddNameOptional : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.BadgeResources", "creator_id", "dbo.Users");
            DropIndex("dbo.BadgeResources", new[] { "creator_id" });
            AlterColumn("dbo.BadgeResources", "creator_id", c => c.Long());
            CreateIndex("dbo.BadgeResources", "creator_id");
            AddForeignKey("dbo.BadgeResources", "creator_id", "dbo.Users", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.BadgeResources", "creator_id", "dbo.Users");
            DropIndex("dbo.BadgeResources", new[] { "creator_id" });
            AlterColumn("dbo.BadgeResources", "creator_id", c => c.Long(nullable: false));
            CreateIndex("dbo.BadgeResources", "creator_id");
            AddForeignKey("dbo.BadgeResources", "creator_id", "dbo.Users", "Id", cascadeDelete: true);
        }
    }
}
