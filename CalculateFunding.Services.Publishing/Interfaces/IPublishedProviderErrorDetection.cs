using CalculateFunding.Models.Publishing;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderErrorDetection
    {
        //TODO; change this to be explicit about error checking call site
        
        //e.g. ProcessPublishedProviderPostProfiling
        
        //TODO; change this signature to remove the predicate func and make pre / post or both a property of the
        //error check itself
        Task ApplyAssignProfilePatternErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context = null);

        //e.g. ProcessPublishedProviderPreProfiling
        Task ApplyRefreshPreVariationErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context);

        Task ApplyRefreshPostVariationsErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context);
    }
}