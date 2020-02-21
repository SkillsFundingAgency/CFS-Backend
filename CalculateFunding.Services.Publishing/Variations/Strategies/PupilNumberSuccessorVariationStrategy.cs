using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class PupilNumberSuccessorVariationStrategy : IVariationStrategy
    {
        private const string Closed = "Closed";

        public string Name => "PupilNumberSuccessor";

        public Task DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            if (providerVariationContext.ReleasedState.Provider.Status != Closed
                && providerVariationContext.UpdatedProvider.Status == Closed
                && string.IsNullOrWhiteSpace(providerVariationContext.UpdatedProvider.Successor))
            {
                // Lookup template calculations based on funding stream and template version (cache based on funding stream and version)
                // Get all calculations with type = pupil number

                // Foreach through all pupil number calculations
                // Set the amount on the successor provider
                // eg providerVariationContext.IncreaseCalculationValueForProvider()

            }
            return Task.CompletedTask;
        }
    }
}
