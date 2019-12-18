using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Providers
{
    public class DeletePublishedProvidersService : IDeletePublishedProvidersService
    {
        private readonly ICreateDeletePublishedProvidersJobs _jobs;

        public DeletePublishedProvidersService(ICreateDeletePublishedProvidersJobs jobs)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            
            _jobs = jobs;
        }

        public async Task QueueDeletePublishedProvidersJob(string fundingStreamId,
            string fundingPeriodId,
            Reference user,
            string correlationId)
        {
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.ArgumentNotNull(user, nameof(user));
            
            await _jobs.CreateJob(fundingStreamId,
                fundingPeriodId,
                user,
                correlationId);
        }
    }
}