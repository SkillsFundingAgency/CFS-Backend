using CalculateFunding.Services.Users.Interfaces;
using Polly;

namespace CalculateFunding.Services.Users
{
    public static class UsersResilienceTestHelper
    {
        public static IUsersResiliencePolicies GenerateTestPolicies()
        {
            return new UsersResiliencePolicies()
            {
                CacheProviderPolicy = Policy.NoOpAsync(),
                FundingStreamPermissionVersionRepositoryPolicy = Policy.NoOpAsync(),
                SpecificationApiClient = Policy.NoOpAsync(),
                UserRepositoryPolicy = Policy.NoOpAsync(),
                UsersSearchRepository = Policy.NoOpAsync(),
                BlobClient = Policy.NoOpAsync()
            };
        }
    }
}
