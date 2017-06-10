namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPastaTimesUsed : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GlobalPastas", "TimesUsed", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.GlobalPastas", "TimesUsed");
        }
    }
}
