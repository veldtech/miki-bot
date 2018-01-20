namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addlevelrolerequiredrole : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LevelRoles", "RequiredRole", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.LevelRoles", "RequiredRole");
        }
    }
}
