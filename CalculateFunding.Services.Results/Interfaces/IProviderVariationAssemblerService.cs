using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderVariationAssemblerService
    {
        Task<IEnumerable<ProviderChangeItem>> AssembleProviderVariationItems(IEnumerable<ProviderResult> providerResults, string specificationId);
    }
}
