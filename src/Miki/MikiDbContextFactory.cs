using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Miki.Bot.Models;

namespace Miki
{
    public class MikiDbContextFactory : IDesignTimeDbContextFactory<MikiDbContext>
    {
        public MikiDbContext CreateDbContext(params string[] args)
        {
            var builder = new DbContextOptionsBuilder<MikiDbContext>();
            builder.UseNpgsql(Environment.GetEnvironmentVariable(Constants.ENV_ConStr), b => b.MigrationsAssembly("Miki.Bot.Models"));
            return new MikiDbContext(builder.Options);
        }
    }
}
