using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface ICalcsRepository
    {
        Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId);

        Task<BuildProject> UpdateBuildProjectRelationships(string specificationId, DatasetRelationshipSummary datasetRelationshipSummary);

        Task<IEnumerable<CalculationResponseModel>> GetCurrentCalculationsBySpecificationId(string specificationId);

        Task<HttpStatusCode> CompileAndSaveAssembly(string specificationId);
    }
}
