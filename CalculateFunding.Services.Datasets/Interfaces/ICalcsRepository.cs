using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.Calcs;
using ObsoleteItem = CalculateFunding.Common.ApiClient.Calcs.Models.ObsoleteItems.ObsoleteItem;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface ICalcsRepository
    {
        Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId);

        Task<BuildProject> UpdateBuildProjectRelationships(string specificationId, DatasetRelationshipSummary datasetRelationshipSummary);

        Task<IEnumerable<CalculationResponseModel>> GetCurrentCalculationsBySpecificationId(string specificationId);

        Task<IEnumerable<ObsoleteItem>> GetObsoleteItemsForSpecification(string specificationId);

        Task<HttpStatusCode> RemoveObsoleteItem(string obsoleteItemId, string calculationId);
        Task<HttpStatusCode> CompileAndSaveAssembly(string specificationId);

        Task<Job> ReMapSpecificationReference(string specificationId, string datasetRelationshipId);

        Task<ObsoleteItem> CreateObsoleteItem(ObsoleteItem obsoleteItem);

        Task<HttpStatusCode> AddCalculationToObsoleteItem(string obsoleteItemId, string calculationId);
        Task<TemplateMapping> GetTemplateMapping(string specificationId, string fundingStreamId);
    }
}
