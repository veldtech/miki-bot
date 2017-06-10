namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSettings : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Settings",
                c => new
                    {
                        EntityId = c.Long(nullable: false),
                        EntityType = c.Int(nullable: false),
                        SettingId = c.Short(nullable: false),
                        IsEnabled = c.Boolean(nullable: false, defaultValue: true),
                    })
                .PrimaryKey(t => new { t.EntityId, t.EntityType, t.SettingId });
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Settings");
        }
    }
}
