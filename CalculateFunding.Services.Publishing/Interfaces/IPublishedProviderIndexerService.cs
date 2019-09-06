using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderIndexerService
    {
        Task IndexPublishedProvider(PublishedProviderVersion publishedProviderVersion);
    }
}
