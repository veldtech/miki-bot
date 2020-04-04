namespace Miki.Tests
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.EntityFrameworkCore;
    using System;
    using Microsoft.Data.Sqlite;
    using Miki.Bot.Models;
    using Miki.Cache;
    using Miki.Cache.InMemory;
    using Miki.Framework;
    using Miki.Serialization.Protobuf;

    public class BaseEntityTest : BaseEntityTest<MikiDbContext>
    {
        public BaseEntityTest()
            : base(x => new MikiDbContext(x))
        {}
    }

    /// <summary>
    /// Creates a test environment with a single-column sqlite database.
    /// </summary>
    public class BaseEntityTest<T>
        where T : DbContext
    {
        public ICacheClient CacheClient { get; }

        private readonly Func<DbContextOptions<T>, T> factory;
        private readonly DbContextOptions<T> options;

        public BaseEntityTest(
            Func<DbContextOptions<T>, T> factory)
        {
            this.factory = factory;
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            options = new DbContextOptionsBuilder<T>().UseSqlite(connection).Options;

            using var context = NewDbContext();
            context.Database.EnsureCreated();

            CacheClient = new InMemoryCacheClient(new ProtobufSerializer());
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
