using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDetectPublishedProviderErrors
    {
        Task DetectErrors(PublishedProviderVersion publishedProviderVersion);
    }
}