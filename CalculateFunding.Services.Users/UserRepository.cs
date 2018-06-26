using CalculateFunding.Models.Users;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Users.Interfaces;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public UserRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<User> GetUserById(string id)
        {
            DocumentEntity<User> user = await _cosmosRepository.ReadAsync<User>(id);

            if (user == null)
                return null;

            return user.Content;
        }

        public Task<HttpStatusCode> SaveUser(User user)
        {
            return _cosmosRepository.UpsertAsync(user);
        }
    }
}
