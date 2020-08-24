using CalculateFunding.Models.Publishing;
using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderErrorDetection
    {
        Task ProcessPublishedProvider(PublishedProvider publishedProvider,
            Func<IDetectPublishedProviderErrors, bool> predicate,
            PublishedProvidersContext context = null);

        Task ProcessPublishedProvider(PublishedProvider publishedProvider,
            PublishedProvidersContext context);
    }
}