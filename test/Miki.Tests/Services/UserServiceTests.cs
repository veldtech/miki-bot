using System.Collections.Generic;
using System.Threading.Tasks;
using Miki.Bot.Models;
using Miki.Services;
using Xunit;

namespace Miki.Tests.Services
{
    public class UserServiceTests : BaseEntityTest
    {
        [Fact]
        public async Task GetUserTestAsync()
        {
            var user = new User
            {
                Id = 1L
            };

            await using(var context = NewContext())
            {
                var repository = context.GetRepository<User>();
                await repository.AddAsync(user);
                await context.CommitAsync();
            }

            await using(var context = NewContext())
            {
                var service = new UserService(context, CacheClient);
                var fetchedUser = await service.GetUserAsync(user.Id);

                Assert.NotNull(fetchedUser);
                Assert.Equal(user.Id, fetchedUser.Id);
            }
        }

        [Fact]
        public async Task GiveReputationTestAsync()
        {
            var sender = new User
            {
                Id = 1L
            };
            var receiver = new User
            {
                Id = 2L
            };

            await using(var context = NewContext())
            {
                var repository = context.GetRepository<User>();
                await repository.AddAsync(sender);
                await repository.AddAsync(receiver);
                await context.CommitAsync();
            }

            await using(var context = NewContext())
            {
                var service = new UserService(context, CacheClient);
                var result = await service.GiveReputationAsync(sender.Id, new List<GiveReputationRequest>
                {
                    new GiveReputationRequest
                    {
                        Amount = 1,
                        RecieverId = receiver.Id
                    }
                });

                Assert.NotNull(result);
                Assert.Equal(2, result.ReputationLeft);
                Assert.NotEmpty(result.UsersReceived);
                Assert.Contains(result.UsersReceived, x => x.Id == receiver.Id);
            }
        }
    }
}
