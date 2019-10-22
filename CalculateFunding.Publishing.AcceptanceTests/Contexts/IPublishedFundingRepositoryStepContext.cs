using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IPublishedFundingRepositoryStepContext
    {
        InMemoryPublishedFundingRepository Repo { get; set; }
        PublishedProvider CurrentPublishedProvider { get; set; }

        ICosmosRepository CosmosRepo { get; set; }

        InMemoryBlobClient BlobRepo { get; set; }
    }
}
