using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Users;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Users.Interfaces;

namespace CalculateFunding.Services.Users
{
    public class UserRepository : IUserRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public UserRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }


        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            var cosmosHealth = await _cosmosRepository.IsHealthOk();

            health.Name = nameof(UserRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosHealth.Ok, DependencyName = this.GetType().Name, Message = cosmosHealth.Message });

            return health;
        }

        public async Task<User> GetUserById(string id)
        {
            IEnumerable<User> user = await _cosmosRepository.QueryPartitionedEntity<User>($"SELECT * FROM Root r WHERE r.content.userId = '{id}' AND r.documentType = '{nameof(User)}' and r.deleted = false", partitionEntityId: id);

            if (!user.AnyWithNullCheck())
            {
                return null;
            }


            return user.Single();
        }

        public Task<HttpStatusCode> SaveUser(User user)
        {
            return _cosmosRepository.UpsertAsync(user, user.UserId);
        }
    }
}
