namespace Miki.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class sqlservertopostgres : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.LocalExperience", "LastExperienceTime", c => c.DateTime(nullable: false));
            AlterColumn("dbo.GuildUsers", "LastRivalRenewed", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Marriages", "TimeOfMarriage", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Marriages", "TimeOfProposal", c => c.DateTime(nullable: false));
            AlterColumn("dbo.GlobalPastas", "DateCreated", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Timers", "Value", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Users", "LastDailyTime", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Users", "DateCreated", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Users", "LastReputationGiven", c => c.DateTime(nullable: false));
        }

        public override void Down()
        {
            AlterColumn("dbo.Users", "LastReputationGiven", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Users", "DateCreated", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Users", "LastDailyTime", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Timers", "Value", c => c.DateTime(nullable: false));
            AlterColumn("dbo.GlobalPastas", "DateCreated", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Marriages", "TimeOfProposal", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Marriages", "TimeOfMarriage", c => c.DateTime(nullable: false));
            AlterColumn("dbo.GuildUsers", "LastRivalRenewed", c => c.DateTime(nullable: false));
            AlterColumn("dbo.LocalExperience", "LastExperienceTime", c => c.DateTime(nullable: false));
        }
    }
}