using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public interface IExistingReleasedProvidersLoadService
    {
        Task LoadExistingReleasedProviders(string specificationId, IEnumerable<string> providerIds);
    }
}