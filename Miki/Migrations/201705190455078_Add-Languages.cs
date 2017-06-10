namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLanguages : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ChannelLanguage",
                c => new
                    {
                        EntityId = c.Long(nullable: false),
                        Language = c.String(defaultValue: "en-us"),
                    })
                .PrimaryKey(t => t.EntityId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ChannelLanguage");
        }
    }
}
