using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class DeleteSpecificationService : IDeleteSpecifications
    {
        private readonly ICreateDeleteSpecificationJobs _jobs;

        public DeleteSpecificationService(ICreateDeleteSpecificationJobs jobs)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            
            _jobs = jobs;
        }

        public async Task QueueDeleteSpecificationJob(string specificationId,
            Reference user,
            string correlationId)
        {
            await _jobs.CreateJob(specificationId,
                user,
                correlationId);
        }
    }
}