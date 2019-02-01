using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderService
    {
        Task<IEnumerable<ProviderSummary>> FetchCoreProviderData();
    }
}
