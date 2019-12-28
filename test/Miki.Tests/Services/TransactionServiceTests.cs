namespace Miki.Tests.Services
{
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Services;
    using Miki.Services.Transactions;
    using Xunit;

    public class TransactionContext : DbContext
    {
        public TransactionContext(DbContextOptions options)
            : base(options)
        { }

        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<LocalExperience> Experience { get; set; }
        public DbSet<GlobalPasta> Pastas { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<CommandUsage> CommandUsages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(x => x.Id);
            modelBuilder.Entity<Achievement>()
                .HasKey(x => new {x.UserId, x.Name});
            modelBuilder.Entity<LocalExperience>()
                .HasKey(x => new {x.UserId, x.ServerId});
            modelBuilder.Entity<Item>()
                .HasKey(x => new { x.Id, x.UserId });
            modelBuilder.Entity<Connection>()
                .HasKey(x => x.UserId);
            modelBuilder.Entity<CommandUsage>()
                .HasKey(x => x.UserId);
        }
    }

    public class TransactionServiceTests : BaseEntityTest<TransactionContext>
    {
        /// <inheritdoc />
        public TransactionServiceTests()
            : base(options => new TransactionContext(options))
        {
            using var ctx = NewDbContext();
            ctx.Users.Add(new User
            {
                Id = 1,
                Currency = 10
            });
            ctx.Users.Add(new User
            {
                Id = 2,
                Currency = 0
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task TransferTest()    
        {
            var unit = NewContext();
            var userService = new UserService(unit);

            var service = new TransactionService(userService, null);
            await service.CreateTransactionAsync(new TransactionRequest(1L, 2L, 10));

            var user1 = await userService.GetUserAsync(1L);
            Assert.NotNull(user1);
            Assert.Equal(0, user1.Currency);

            var user2 = await userService.GetUserAsync(2L);
            Assert.NotNull(user2);
            Assert.Equal(10, user2.Currency);
        }

        [Fact]
        public async Task InvalidCurrencyTransferTest()
        {
            var unit = NewContext();
            var userService = new UserService(unit);

            var service = new TransactionService(userService, null);
            await Assext.ThrowsRootAsync<InsufficientCurrencyException>(
                async () => await service.CreateTransactionAsync(new TransactionRequest(2L, 1L, 10)));
        }

        [Fact]
        public async Task InvalidSelfTransferTest()
        {
            var unit = NewContext();
            var userService = new UserService(unit);

            var service = new TransactionService(userService, null);
            await Assext.ThrowsRootAsync<UserNullException>(
                async () => await service.CreateTransactionAsync(new TransactionRequest(1L, 1L, 10)));
        }
    }
}
