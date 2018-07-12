using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderImportMappingService
    {
        ProviderIndex Map(MasterProviderModel masterProviderModel);
    }
}
