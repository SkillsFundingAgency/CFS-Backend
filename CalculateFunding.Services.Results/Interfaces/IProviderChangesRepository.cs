using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Providers;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderChangesRepository
    {
        Task AddProviderChanges(IEnumerable<ProviderChangeRecord> providerChangeRecords);
    }
}
