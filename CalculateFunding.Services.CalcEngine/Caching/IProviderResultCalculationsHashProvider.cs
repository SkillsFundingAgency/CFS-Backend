using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.CalcEngine.Caching
{
    public interface IProviderResultCalculationsHashProvider
    {
        bool TryUpdateCalculationResultHash(ProviderResult providerResult,
            int partitionIndex,
            int partitionSize);

        void StartBatch(string specificationId,
            int partitionIndex,
            int partitionSize);

        void EndBatch(string specificationId,
            int partitionIndex,
            int partitionSize);
    }
}