namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RevertMarriagesToBase : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Marriages", "Id1IsProposing");
            DropColumn("dbo.Marriages", "TimesRemarried");
            DropColumn("dbo.Marriages", "Divorced");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Marriages", "Divorced", c => c.Boolean(nullable: false));
            AddColumn("dbo.Marriages", "TimesRemarried", c => c.Int(nullable: false));
            AddColumn("dbo.Marriages", "Id1IsProposing", c => c.Boolean(nullable: false));
        }
    }
}
