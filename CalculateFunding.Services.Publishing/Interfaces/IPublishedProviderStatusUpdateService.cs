using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderStatusUpdateService
    {
        Task<int> UpdatePublishedProviderStatus(IEnumerable<PublishedProvider> publishedProviders, 
            Reference author, 
            PublishedProviderStatus publishedProviderStatus, 
            string jobId = null,
            string correlationId = null);
    }
}
