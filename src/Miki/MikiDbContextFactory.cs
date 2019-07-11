using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Miki.Bot.Models;
using Newtonsoft.Json;
using System;

namespace Miki
{
    public class MikiDbContextFactory : IDesignTimeDbContextFactory<MikiDbContext>
    {
        public MikiDbContext CreateDbContext(params string[] args)
        {
            var builder = new DbContextOptionsBuilder<MikiDbContext>();
            builder.UseNpgsql(Global.Config.ConnString, b => b.MigrationsAssembly("Miki.Bot.Models"));
            return new MikiDbContext(builder.Options);
        }
    }
}
