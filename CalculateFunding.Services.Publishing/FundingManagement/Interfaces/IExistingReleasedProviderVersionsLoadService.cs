using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public interface IExistingReleasedProviderVersionsLoadService
    {
        Task LoadExistingReleasedProviderVersions(string specificationId, IEnumerable<string> providerIds,
            IEnumerable<string> channelCodes);
    }
}