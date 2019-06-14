using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.Azure.Documents;
using User = CalculateFunding.Models.Users.User;

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
            Guard.IsNullOrWhiteSpace(id, nameof(id));

            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = "SELECT * FROM Root r WHERE r.content.userId = @UserId AND r.documentType = @DocumentType AND r.deleted = false",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@UserId", id), 
                    new SqlParameter("@DocumentType", nameof(User))
                }
            };
            IEnumerable<User> user = await _cosmosRepository.QueryPartitionedEntity<User>(sqlQuerySpec, partitionEntityId: id);

            if (!user.AnyWithNullCheck()) return null;

            return user.Single();
        }

        public Task<HttpStatusCode> SaveUser(User user)
        {
            Guard.ArgumentNotNull(user, nameof(user));

            return _cosmosRepository.UpsertAsync(user, user.UserId);
        }

        public async Task<FundingStreamPermission> GetFundingStreamPermission(string userId, string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(userId, nameof(userId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = @"SELECT *
                            FROM    Root r
                            WHERE   r.content.fundingStreamId = @FundingStreamID
                                    AND r.documentType = @DocumentType
                                    AND r.deleted = false",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@FundingStreamID", fundingStreamId),
                    new SqlParameter("@DocumentType", nameof(FundingStreamPermission))
                }
            };
            
            IEnumerable<FundingStreamPermission> permission = await _cosmosRepository.QueryPartitionedEntity<FundingStreamPermission>(sqlQuerySpec, partitionEntityId: userId);

            if (!permission.AnyWithNullCheck())
            {
                return null;
            }

            return permission.Single();
        }

        public async Task<HttpStatusCode> UpdateFundingStreamPermission(FundingStreamPermission fundingStreamPermission)
        {
            Guard.ArgumentNotNull(fundingStreamPermission, nameof(fundingStreamPermission));

            return await _cosmosRepository.UpsertAsync(fundingStreamPermission, fundingStreamPermission.UserId);
        }

        public async Task<IEnumerable<FundingStreamPermission>> GetFundingStreamPermissions(string userId)
        {
            Guard.IsNullOrWhiteSpace(userId, nameof(userId));

            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = @"SELECT * FROM Root r WHERE r.content.userId = @UserId AND r.documentType = @DocumentType AND r.deleted = false",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@DocumentType", nameof(FundingStreamPermission))
                }
            };
            
            return await _cosmosRepository.QueryPartitionedEntity<FundingStreamPermission>(sqlQuerySpec, partitionEntityId: userId);
        }

        public async Task<IEnumerable<FundingStreamPermission>> GetUsersWithFundingStreamPermissions(string fundingStreamId)
        {
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = @"SELECT * 
                            FROM    Root r 
                            WHERE   r.content.fundingStreamId = @FundingStreamID 
                                    AND r.documentType = @DocumentType
                                    AND r.deleted = false",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@FundingStreamID", fundingStreamId),
                    new SqlParameter("@DocumentType", nameof(FundingStreamPermission))
                }
            };
            return await _cosmosRepository.QuerySql<FundingStreamPermission>(sqlQuerySpec, enableCrossPartitionQuery: true);
        }
    }
}
