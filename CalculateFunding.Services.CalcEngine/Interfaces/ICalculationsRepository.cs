using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface ICalculationsRepository
    {
        Task<IEnumerable<CalculationSummaryModel>> GetCalculationSummariesForSpecification(string specificationId);

        Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId);
    }
}
