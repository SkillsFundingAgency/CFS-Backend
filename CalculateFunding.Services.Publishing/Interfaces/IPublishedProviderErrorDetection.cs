using CalculateFunding.Models.Publishing;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderErrorDetection
    {
        //TODO; change this to be explicit about error checking call site
        
        //e.g. ProcessPublishedProviderPostProfiling
        
        Task<bool> ApplyAssignProfilePatternErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context);

        //e.g. ProcessPublishedProviderPreProfiling
        Task<bool> ApplyRefreshPreVariationErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context);

        Task<bool> ApplyRefreshPostVariationsErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context);
    }
}