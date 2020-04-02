using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class InstructionAllocationJobCreation : IInstructionAllocationJobCreation
    {
        private readonly AsyncPolicy _calculationRepositoryPolicy;
        private readonly AsyncPolicy _jobsApiClientPolicy;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly ILogger _logger;
        
        public InstructionAllocationJobCreation(ICalculationsRepository calculationsRepository, 
            ICalcsResiliencePolicies calculationsResiliencePolicies,
            ILogger logger, 
            IJobsApiClient jobsApiClient)
        {
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.JobsApiClient, nameof(calculationsResiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.CalculationsRepository, nameof(calculationsResiliencePolicies.CalculationsRepository));
            
            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _jobsApiClient = jobsApiClient;
            _calculationRepositoryPolicy = calculationsResiliencePolicies.CalculationsRepository;
            _jobsApiClientPolicy = calculationsResiliencePolicies.JobsApiClient;
        }

        public async Task<Job> SendInstructAllocationsToJobService(string specificationId, string userId, string userName, Trigger trigger, string correlationId, bool initiateCalcRUn = true)
        {
            IEnumerable<Calculation> allCalculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

            bool generateCalculationAggregations = SourceCodeHelpers.HasCalculationAggregateFunctionParameters(allCalculations.Select(m => m.Current.SourceCode));

            JobCreateModel job = new JobCreateModel
            {
                InvokerUserDisplayName = userName,
                InvokerUserId = userId,
                JobDefinitionId = generateCalculationAggregations ?
                    JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob :
                    JobConstants.DefinitionNames.CreateInstructAllocationJob,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>
                {
                    { "specification-id", specificationId }
                },
                Trigger = trigger,
                CorrelationId = correlationId
            };

            try
            {
                return await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.CreateJob(job));
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to create job of type '{job.JobDefinitionId}' on specification '{specificationId}'";

                _logger.Error(ex, errorMessage);

                throw new RetriableException(errorMessage, ex);
            }
        }    
    }
}