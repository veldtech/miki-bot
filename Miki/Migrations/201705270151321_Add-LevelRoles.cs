namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLevelRoles : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LevelRoles",
                c => new
                    {
                        GuildId = c.Long(nullable: false),
                        RoleId = c.Long(nullable: false),
                        RequiredLevel = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.GuildId, t.RoleId });
            
        }
        
        public override void Down()
        {
            DropTable("dbo.LevelRoles");
        }
    }
}
