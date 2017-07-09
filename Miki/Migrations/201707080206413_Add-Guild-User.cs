namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGuildUser : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.GuildUser",
                c => new
                    {
                        EntityId = c.Long(nullable: false),
                        Experience = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.EntityId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.GuildUser");
        }
    }
}
