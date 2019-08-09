using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ICalculationResultsRepository
    {
        Task<IEnumerable<ProviderCalculationResult>> GetCalculationResultsBySpecificationId(string specificationId);
    }
}
