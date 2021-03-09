using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
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
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;
        private readonly ICalculationsFeatureFlag _calculationsFeatureFlag;
        private bool? _graphEnabled;
        public InstructionAllocationJobCreation(ICalculationsRepository calculationsRepository, 
            ICalcsResiliencePolicies calculationsResiliencePolicies,
            ILogger logger,
            ICalculationsFeatureFlag calculationsFeatureFlag,
            IJobManagement jobManagement)
        {
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(calculationsFeatureFlag, nameof(calculationsFeatureFlag));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.CalculationsRepository, nameof(calculationsResiliencePolicies.CalculationsRepository));

            _calculationsFeatureFlag = calculationsFeatureFlag;
            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _jobManagement = jobManagement;
            _calculationRepositoryPolicy = calculationsResiliencePolicies.CalculationsRepository;
        }

        public async Task<Job> SendInstructAllocationsToJobService(string specificationId, string userId, string userName, Trigger trigger, string correlationId, bool initiateCalcRUn = true)
        {
            Job parentJob = null;
            
            IEnumerable<Calculation> allCalculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

            bool generateCalculationAggregations = SourceCodeHelpers.HasCalculationAggregateFunctionParameters(allCalculations.Select(m => m.Current.SourceCode));

            string jobDefinitionId = generateCalculationAggregations ?
                    JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob :
                    JobConstants.DefinitionNames.CreateInstructAllocationJob;

            if (await GraphEnabled())
            {
                // if the graph is enabled then we need to queue a reindex of the graph
                jobDefinitionId = JobConstants.DefinitionNames.ReIndexSpecificationCalculationRelationshipsJob;
            }

            JobCreateModel job = new JobCreateModel
            { 
                InvokerUserDisplayName = userName,
                InvokerUserId = userId,
                JobDefinitionId = jobDefinitionId,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>
                {
                    { "specification-id", specificationId }
                },
                Trigger = trigger,
                CorrelationId = correlationId
            };
            

            if (await GraphEnabled())
            {
                string parentJobDefinition = generateCalculationAggregations ?
                            JobConstants.DefinitionNames.GenerateGraphAndInstructGenerateAggregationAllocationJob :
                            JobConstants.DefinitionNames.GenerateGraphAndInstructAllocationJob;

                try
                {
                    JobCreateModel instructJob = new JobCreateModel
                    {
                        InvokerUserDisplayName = userName,
                        InvokerUserId = userId,
                        JobDefinitionId = parentJobDefinition,
                        SpecificationId = specificationId,
                        Properties = new Dictionary<string, string>
                        {
                            { "specification-id", specificationId }
                        },
                        Trigger = trigger,
                        CorrelationId = correlationId
                    };
                    
                    parentJob = await _jobManagement.QueueJob(instructJob);

                    _logger.Information($"New job of type '{parentJob.JobDefinitionId}' created with id: '{parentJob.Id}'");
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Failed to create job of type '{parentJobDefinition}' on specification '{specificationId}'";

                    _logger.Error(ex, errorMessage);

                    throw new RetriableException(errorMessage, ex);
                }
            }

            if (parentJob != null)
            {
                job.ParentJobId = parentJob.Id;
            }

            try
            {
                return await _jobManagement.QueueJob(job);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to create job of type '{job.JobDefinitionId}' on specification '{specificationId}'";
                
                _logger.Error(ex, errorMessage);

                throw new RetriableException(errorMessage, ex);
            }
        }

        private async Task<bool> GraphEnabled()
        {
            _graphEnabled ??= await _calculationsFeatureFlag.IsGraphEnabled();

            return _graphEnabled.Value;
        }
    }
}