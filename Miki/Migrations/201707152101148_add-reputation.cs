namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addreputation : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "Reputation", c => c.Int(nullable: false));
            AddColumn("dbo.Users", "LastReputationGiven", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "LastReputationGiven");
            DropColumn("dbo.Users", "Reputation");
        }
    }
}
