namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixMarriage : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Marriages", "Id1IsProposing", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Marriages", "Id1IsProposing");
        }
    }
}
