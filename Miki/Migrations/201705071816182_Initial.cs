namespace Miki.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Achievements",
                c => new
                    {
                        Id = c.Long(nullable: false),
                        Name = c.String(nullable: false, maxLength: 128),
                        Rank = c.Short(nullable: false),
                    })
                .PrimaryKey(t => new { t.Id, t.Name });
            
            CreateTable(
                "dbo.EventMessages",
                c => new
                    {
                        ChannelId = c.Long(nullable: false),
                        EventType = c.Short(nullable: false),
                        Message = c.String(),
                    })
                .PrimaryKey(t => new { t.ChannelId, t.EventType });
            
            CreateTable(
                "dbo.LocalExperience",
                c => new
                    {
                        ServerId = c.Long(nullable: false),
                        UserId = c.Long(nullable: false),
                        Experience = c.Int(nullable: false),
                        LastExperienceTime = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.ServerId, t.UserId });
            
            CreateTable(
                "dbo.GlobalPastas",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Content = c.String(),
                        CreatorID = c.Long(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Long(nullable: false),
                        Name = c.String(),
                        Title = c.String(),
                        Total_Commands = c.Int(nullable: false),
                        Total_Experience = c.Int(nullable: false),
                        Currency = c.Int(nullable: false),
                        MarriageSlots = c.Int(nullable: false),
                        AvatarUrl = c.String(),
                        HeaderUrl = c.String(),
                        LastDailyTime = c.DateTime(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Votes",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        UserId = c.Long(nullable: false),
                        PositiveVote = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.Id, t.UserId });
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Votes");
            DropTable("dbo.Users");
            DropTable("dbo.GlobalPastas");
            DropTable("dbo.LocalExperience");
            DropTable("dbo.EventMessages");
            DropTable("dbo.Achievements");
        }
    }
}
