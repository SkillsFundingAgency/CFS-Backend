using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ICreateDeletePublishedProvidersJobs
    {
        Task<Job> CreateJob(string fundingStreamId,
            string fundingPeriodId,
            Reference user,
            string correlationId);
    }
}