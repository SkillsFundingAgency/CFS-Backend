using System;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Calcs.Analysis
{
    public class ReIndexSpecificationCalculationRelationships : IReIndexSpecificationCalculationRelationships
    {
        private readonly ISpecificationCalculationAnalysis _analysis;
        private readonly IReIndexGraphRepository _reIndexGraphs;
        private readonly IJobManagement _jobManagement;

        private readonly ILogger _logger;

        private const string SpecificationId = "specification-id";

        public ReIndexSpecificationCalculationRelationships(ISpecificationCalculationAnalysis analysis,
            IReIndexGraphRepository reIndexGraphs,
            ILogger logger,
            IJobManagement jobManagement)
        {
            Guard.ArgumentNotNull(analysis, nameof(analysis));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(reIndexGraphs, nameof(reIndexGraphs));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            
            _analysis = analysis;
            _logger = logger;
            _reIndexGraphs = reIndexGraphs;
            _jobManagement = jobManagement;
        }

        public async Task Run(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = message.GetUserProperty<string>(SpecificationId);
            string jobId = message.GetUserProperty<string>("jobId");

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            try
            {
                // Update job to set status to processing
                await _jobManagement.UpdateJobStatus(jobId, 0, 0, null, null);

                SpecificationCalculationRelationships specificationCalculationRelationships = await _analysis
                    .GetSpecificationCalculationRelationships(specificationId);

                SpecificationCalculationRelationships specificationCalculationUnusedRelationships = await _reIndexGraphs.GetUnusedRelationships(specificationCalculationRelationships);

                await _reIndexGraphs.RecreateGraph(specificationCalculationRelationships, specificationCalculationUnusedRelationships);

                await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to reindex specification calculation relationships");
                
                throw;
            }
        }
    }
}