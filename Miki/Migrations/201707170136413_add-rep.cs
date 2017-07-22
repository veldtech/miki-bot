namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addrep : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "ReputationPointsLeft", c => c.Short(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "ReputationPointsLeft");
        }
    }
}
