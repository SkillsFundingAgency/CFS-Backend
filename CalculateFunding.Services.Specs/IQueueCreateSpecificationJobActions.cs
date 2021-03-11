using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Specs
{
    public interface IQueueCreateSpecificationJobActions
    {
        Task<ActionResult<Job>> CreateProviderSnapshotDataLoadJob(ProviderSnapshotDataLoadRequest request, Reference user, string correlationId);
        Task Run(SpecificationVersion specificationVersion, Reference user, string correlationId);
    }
}