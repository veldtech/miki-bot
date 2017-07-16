namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addconfigtoguilduser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GuildUsers", "MinimalExperienceToGetRewards", c => c.Int(nullable: false));
            AddColumn("dbo.GuildUsers", "VisibleOnLeaderboards", c => c.Boolean(nullable: false));
            DropColumn("dbo.GuildUsers", "LastRewardClaimed");
        }
        
        public override void Down()
        {
            AddColumn("dbo.GuildUsers", "LastRewardClaimed", c => c.DateTime(nullable: false));
            DropColumn("dbo.GuildUsers", "VisibleOnLeaderboards");
            DropColumn("dbo.GuildUsers", "MinimalExperienceToGetRewards");
        }
    }
}
