namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addpastascore : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GlobalPastas", "score", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.GlobalPastas", "score");
        }
    }
}
