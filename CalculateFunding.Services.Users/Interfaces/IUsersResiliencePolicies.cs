using Polly;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUsersResiliencePolicies
    {
        Policy UserRepositoryPolicy { get; set; }

        Policy SpecificationApiClient { get; set; }

        Policy FundingStreamPermissionVersionRepositoryPolicy { get; set; }

        Policy CacheProviderPolicy { get; set; }
    }
}
