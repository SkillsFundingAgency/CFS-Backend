using Polly;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUsersResiliencePolicies
    {
        AsyncPolicy UserRepositoryPolicy { get; set; }

        AsyncPolicy SpecificationApiClient { get; set; }

        AsyncPolicy FundingStreamPermissionVersionRepositoryPolicy { get; set; }

        AsyncPolicy CacheProviderPolicy { get; set; }

        AsyncPolicy UsersSearchRepository { get; set; }

        AsyncPolicy BlobClient { get; set; }

    }
}
