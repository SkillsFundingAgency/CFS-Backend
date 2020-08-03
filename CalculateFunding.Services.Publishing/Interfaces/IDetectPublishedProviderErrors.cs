using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDetectPublishedProviderErrors
    {
        Task DetectErrors(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext);
    }
}