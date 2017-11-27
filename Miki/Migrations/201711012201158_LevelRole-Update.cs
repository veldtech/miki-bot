namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LevelRoleUpdate : DbMigration
    {
        public override void Up()
        {
			AddColumn("dbo.LevelRoles", "Automatic", c => c.Boolean(nullable: false, defaultValue: false));
            AddColumn("dbo.LevelRoles", "Optable", c => c.Boolean(nullable: false, defaultValue: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.LevelRoles", "Optable");
            DropColumn("dbo.LevelRoles", "Automatic");
        }
    }
}
