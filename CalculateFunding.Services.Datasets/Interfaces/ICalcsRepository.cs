using CalculateFunding.Models.Calcs;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface ICalcsRepository
    {
        Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId);

        Task<BuildProject> UpdateBuildProjectRelationships(string specificationId, DatasetRelationshipSummary datasetRelationshipSummary);

        Task<IEnumerable<CalculationCurrentVersion>> GetCurrentCalculationsBySpecificationId(string specificationId);

        Task<HttpStatusCode> CompileAndSaveAssembly(string specificationId);
    }
}
