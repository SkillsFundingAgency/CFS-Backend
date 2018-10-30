using CalculateFunding.Services.Users.Interfaces;
using Polly;

namespace CalculateFunding.Services.Users
{
    public class UsersResiliencePolicies : IUsersResiliencePolicies
    {
        public Policy UserRepositoryPolicy { get; set; }

        public Policy SpecificationRepositoryPolicy { get; set; }

        public Policy FundingStreamPermissionVersionRepositoryPolicy { get; set; }

        public Policy CacheProviderPolicy { get; set; }
    }
}
