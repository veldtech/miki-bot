namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGuildUserTimer : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.GuildUser", newName: "GuildUsers");
            CreateTable(
                "dbo.Timers",
                c => new
                    {
                        GuildId = c.Long(nullable: false),
                        UserId = c.Long(nullable: false),
                        Value = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.GuildId, t.UserId });
            
            AddColumn("dbo.GuildUsers", "LastRivalRenewed", c => c.DateTime(nullable: false));
            AddColumn("dbo.GuildUsers", "LastRewardClaimed", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.GuildUsers", "LastRewardClaimed");
            DropColumn("dbo.GuildUsers", "LastRivalRenewed");
            DropTable("dbo.Timers");
            RenameTable(name: "dbo.GuildUsers", newName: "GuildUser");
        }
    }
}
