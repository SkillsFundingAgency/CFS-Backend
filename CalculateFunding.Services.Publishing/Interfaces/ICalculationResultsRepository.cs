using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ICalculationResultsRepository
    {
        Task<IEnumerable<ProviderCalculationResult>> GetCalculationResultsBySpecificationAndProvider(string specificationId, string providerId);
    }
}
