namespace Miki
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;
    using Miki.Bot.Models;
    using Newtonsoft.Json;
    using System;

    public class MikiDbContextFactory : IDesignTimeDbContextFactory<MikiDbContext>
    {
        public MikiDbContext CreateDbContext(params string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable(Constants.EnvConStr);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Cannot create database context without connection string.");
            }

            var builder = new DbContextOptionsBuilder<MikiDbContext>();
            builder.UseNpgsql(connectionString, b => b.MigrationsAssembly("Miki.Bot.Models"));
            return new MikiDbContext(builder.Options);
        }
    }
}
