using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ICalculationResultsService
    {
        Task<IDictionary<string, ProviderCalculationResult>> GetCalculationResultsBySpecificationId(string specificationId, IEnumerable<string> scopedProviderIds);
    }
}
