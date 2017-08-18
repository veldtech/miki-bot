namespace Miki.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class MigrationConfiguration : DbMigrationsConfiguration<Miki.Models.MikiContext>
    {
        public MigrationConfiguration()
        {
            AutomaticMigrationsEnabled = false;
        }
    }
}