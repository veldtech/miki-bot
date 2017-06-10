namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEntityTypeToEventMessage : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.Settings");
            AlterColumn("dbo.Settings", "SettingId", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.Settings", new[] { "EntityId", "EntityType", "SettingId" });
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.Settings");
            AlterColumn("dbo.Settings", "SettingId", c => c.Short(nullable: false));
            AddPrimaryKey("dbo.Settings", new[] { "EntityId", "EntityType", "SettingId" });
        }
    }
}
