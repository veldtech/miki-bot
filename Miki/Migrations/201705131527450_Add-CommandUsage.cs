namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCommandUsage : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CommandUsages",
                c => new
                    {
                        UserId = c.Long(nullable: false),
                        Name = c.String(nullable: false, maxLength: 128),
                        Amount = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.UserId, t.Name });
            
        }
        
        public override void Down()
        {
            DropTable("dbo.CommandUsages");
        }
    }
}
