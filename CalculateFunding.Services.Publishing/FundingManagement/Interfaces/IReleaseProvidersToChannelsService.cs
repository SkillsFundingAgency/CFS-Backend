using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing.FundingManagement;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public interface IReleaseProvidersToChannelsService
    {
        Task ReleaseProviderVersions(string specificationId, ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest, Reference author, string correlationId);
    }
}