using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Services.Publishing
{
    public interface ICreateRefreshFundingJobs
    {
        Task<Job> CreateJob(string specificationId,
            Reference user,
            string correlationId);
    }
}