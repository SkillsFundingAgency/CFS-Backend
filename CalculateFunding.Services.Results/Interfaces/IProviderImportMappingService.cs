using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderImportMappingService
    {
        ProviderIndex Map(MasterProviderModel masterProviderModel);
    }
}
