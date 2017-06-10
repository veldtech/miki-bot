namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMarriage : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Marriages",
                c => new
                    {
                        Id1 = c.Long(nullable: false),
                        Id2 = c.Long(nullable: false),
                        TimesRemarried = c.Int(nullable: false),
                        Proposing = c.Boolean(nullable: false),
                        Divorced = c.Boolean(nullable: false),
                        TimeOfMarriage = c.DateTime(nullable: false),
                        TimeOfProposal = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.Id1, t.Id2 });
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Marriages");
        }
    }
}
