using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IOutOfScopePublishedProviderBuilder
    {
        Task<PublishedProvider> CreateMissingPublishedProviderForPredecessor(PublishedProvider predecessor,
            string successorId,
            ProviderVariationContext variationContext = null);
    }
}