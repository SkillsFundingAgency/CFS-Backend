using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IPublishedProviderCalculationResultsRepository
    {
        Task CreatePublishedCalculationResults(IEnumerable<PublishedProviderCalculationResult> publishedCalculationResults);
    }
}
