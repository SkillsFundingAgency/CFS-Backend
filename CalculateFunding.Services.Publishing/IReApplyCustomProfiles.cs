using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public interface IReApplyCustomProfiles
    {
        void ProcessPublishedProvider(PublishedProviderVersion publishedProviderVersion);
    }
}