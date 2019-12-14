namespace Miki.Tests
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.EntityFrameworkCore;
    using System;
    using Microsoft.Data.Sqlite;
    using Miki.Framework;

    /// <summary>
    /// Creates a test environment with a single-column sqlite database.
    /// </summary>
    public class BaseEntityTest<T>
        where T : DbContext
    {
        private readonly Func<DbContextOptions<T>, T> factory;
        private readonly DbContextOptions<T> options;
        

        public BaseEntityTest(
            Func<DbContextOptions<T>, T> factory)
        {
            this.factory = factory;
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            options = new DbContextOptionsBuilder<T>()
                .UseSqlite(connection)
                .Options;

            using var context = NewDbContext();
            context.Database.EnsureCreated();
        }

        public IUnitOfWork NewContext()
        {
            return new UnitOfWork(NewDbContext());
        }

        protected T NewDbContext()
        {
            return factory(options);
        }
    }
}
