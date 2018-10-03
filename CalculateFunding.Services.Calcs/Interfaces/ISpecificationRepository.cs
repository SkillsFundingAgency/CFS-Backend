using CalculateFunding.Models.Specs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ISpecificationRepository
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);

        Task<IEnumerable<Calculation>> GetCalculationSpecificationsForSpecification(string specificationId);

        Task<IEnumerable<FundingStream>> GetFundingStreams();
    }
}
