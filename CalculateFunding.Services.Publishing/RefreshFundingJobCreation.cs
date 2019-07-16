using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using Serilog;
using Policy = Polly.Policy;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshFundingJobCreation : ICreateRefreshFundingJobs
    {
        private readonly IJobsApiClient _jobs;
        private readonly ICalcsResiliencePolicies _resiliencePolicies;
        private readonly ILogger _logger;

        public RefreshFundingJobCreation(IJobsApiClient jobs,
            ICalcsResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobs = jobs;
            _resiliencePolicies = resiliencePolicies;
            _logger = logger;
        }

        public async Task<Job> CreateJob(string specificationId,
            Reference user,
            string correlationId)
        {
            try
            {
                return await JobsPolicy.ExecuteAsync(() => _jobs.CreateJob(new JobCreateModel
                {
                    InvokerUserDisplayName = user.Name,
                    InvokerUserId = user.Id,
                    JobDefinitionId = JobConstants.DefinitionNames.CreateRefreshFundingjob,
                    Properties = new Dictionary<string, string>
                    {
                        {"specification-id", specificationId}
                    },
                    SpecificationId = specificationId,
                    Trigger = new Trigger
                    {
                        EntityId = specificationId,
                        EntityType = nameof(Specification),
                        Message = "Requesting publication of specification"
                    },
                    CorrelationId = correlationId
                }));
            }
            catch (Exception ex)
            {
                string error = $"Failed to queue publishing of specification with id: {specificationId}";

                _logger.Error(ex, error);

                throw new Exception(error);
            }
        }

        private Policy JobsPolicy => _resiliencePolicies.JobsApiClient;
    }
}