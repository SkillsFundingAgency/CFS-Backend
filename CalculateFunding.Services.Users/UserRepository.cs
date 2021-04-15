using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Users.Interfaces;
using User = CalculateFunding.Models.Users.User;

namespace CalculateFunding.Services.Users
{
    public class UserRepository : IUserRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;

        public UserRepository(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            (bool Ok, string Message) = _cosmosRepository.IsHealthOk();

            health.Name = nameof(UserRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = this.GetType().Name, Message = Message });

            return Task.FromResult(health);
        }

        public async Task<User> GetUserById(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));

            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = "SELECT * FROM Root r WHERE r.content.userId = @UserId AND r.documentType = @DocumentType AND r.deleted = false",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@UserId", id),
                    new CosmosDbQueryParameter("@DocumentType", nameof(User))
                }
            };
            IEnumerable<User> user = await _cosmosRepository.QueryPartitionedEntity<User>(cosmosDbQuery, partitionKey: id);

            if (!user.AnyWithNullCheck()) return null;

            return user.Single();
        }

        public Task<HttpStatusCode> SaveUser(User user)
        {
            Guard.ArgumentNotNull(user, nameof(user));

            return _cosmosRepository.UpsertAsync(user, user.UserId);
        }

        public async Task<FundingStreamPermission> GetFundingStreamPermission(string userId, string fundingStreamId, bool includeDeleted = false)
        {
            Guard.IsNullOrWhiteSpace(userId, nameof(userId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT *
                            FROM    Root r
                            WHERE   r.content.fundingStreamId = @FundingStreamID
                                    AND r.documentType = @DocumentType" +
                                    (includeDeleted ? string.Empty : " AND r.deleted = false"),
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@FundingStreamID", fundingStreamId),
                    new CosmosDbQueryParameter("@DocumentType", nameof(FundingStreamPermission))
                }
            };

            IEnumerable<FundingStreamPermission> permission = await _cosmosRepository.QueryPartitionedEntity<FundingStreamPermission>(cosmosDbQuery, partitionKey: userId);

            if (!permission.AnyWithNullCheck())
            {
                return null;
            }

            return permission.Single();
        }

        public async Task<HttpStatusCode> UpdateFundingStreamPermission(FundingStreamPermission fundingStreamPermission)
        {
            Guard.ArgumentNotNull(fundingStreamPermission, nameof(fundingStreamPermission));

            return await _cosmosRepository.UpsertAsync(fundingStreamPermission, fundingStreamPermission.UserId, undelete: true);
        }

        public async Task<HttpStatusCode> DeleteFundingStreamPermission(FundingStreamPermission fundingStreamPermission)
        {
            Guard.ArgumentNotNull(fundingStreamPermission, nameof(fundingStreamPermission));
            
            //keep getting a bad request when I use this delete path so using bulk delete method for now
            await _cosmosRepository.BulkDeleteAsync(new[] { new KeyValuePair<string, FundingStreamPermission>(fundingStreamPermission.UserId, fundingStreamPermission) });
            return HttpStatusCode.OK;
        }

        public async Task<IEnumerable<FundingStreamPermission>> GetFundingStreamPermissions(string userId)
        {
            Guard.IsNullOrWhiteSpace(userId, nameof(userId));

            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT * FROM Root r WHERE r.content.userId = @UserId AND r.documentType = @DocumentType AND r.deleted = false",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@UserId", userId),
                    new CosmosDbQueryParameter("@DocumentType", nameof(FundingStreamPermission))
                }
            };

            return await _cosmosRepository.QueryPartitionedEntity<FundingStreamPermission>(cosmosDbQuery, partitionKey: userId);
        }

        public async Task<IEnumerable<FundingStreamPermission>> GetUsersWithFundingStreamPermissions(string fundingStreamId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT * 
                            FROM    Root r 
                            WHERE   r.content.fundingStreamId = @FundingStreamID 
                                    AND r.documentType = @DocumentType
                                    AND r.deleted = false",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@FundingStreamID", fundingStreamId),
                    new CosmosDbQueryParameter("@DocumentType", nameof(FundingStreamPermission))
                }
            };
            return await _cosmosRepository.QuerySql<FundingStreamPermission>(cosmosDbQuery);
        }


        public async Task<IEnumerable<User>> GetUsers()
        {
            return await _cosmosRepository.Query<User>();
        }

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = "SELECT * FROM Root r WHERE r.documentType = @DocumentType AND r.deleted = false",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@DocumentType", nameof(User))
                }
            };
            IEnumerable<User> users = await _cosmosRepository.QuerySql<User>(cosmosDbQuery);

            if (!users.AnyWithNullCheck()) return null;

            return users;
        }
    }
}
