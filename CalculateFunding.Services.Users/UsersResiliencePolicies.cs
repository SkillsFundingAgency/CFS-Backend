using CalculateFunding.Services.Users.Interfaces;
using Polly;

namespace CalculateFunding.Services.Users
{
    public class UsersResiliencePolicies : IUsersResiliencePolicies
    {
        public AsyncPolicy UserRepositoryPolicy { get; set; }

        public AsyncPolicy SpecificationApiClient { get; set; }

        public AsyncPolicy FundingStreamPermissionVersionRepositoryPolicy { get; set; }

        public AsyncPolicy CacheProviderPolicy { get; set; }
        public AsyncPolicy BlobClient { get; set; }
    }
}
