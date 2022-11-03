using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces.Undo
{
    public interface IPublishedFundingUndoBlobStoreRepository
    {
        Task RemovePublishedProviderVersionBlob(PublishedProviderVersion publishedProviderVersion);
        
        Task RemovePublishedFundingVersionBlob(PublishedFundingVersion publishedFundingVersion);

        Task RemoveReleasedGroupBlob(PublishedFundingVersion publishedFundingVersion, string channelCode);

        Task RemoveReleasedprovidersBlob(PublishedProviderVersion publishedProviderVersion, string channelCode);
    }
}