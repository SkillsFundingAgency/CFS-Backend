using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public interface IPostReleaseJobCreationService
    {
        Task QueueJobs(SpecificationSummary specification, string correlationId, Reference author);
    }
}