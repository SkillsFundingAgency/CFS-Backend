using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface ICalculationsRepository
    {
        Task<IEnumerable<CalculationSummaryModel>> GetCalculationSummariesForSpecification(string specificationId);

        Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId);

        Task<byte[]> GetAssemblyBySpecificationId(string specificationId);
    }
}
