using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public interface IReApplyCustomProfiles
    {
        Task ProcessPublishedProvider(PublishedProviderVersion publishedProviderVersion);
    }
}