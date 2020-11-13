using System;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Calcs.Analysis
{
    public class ReIndexSpecificationCalculationRelationships : JobProcessingService, IReIndexSpecificationCalculationRelationships
    {
        private readonly ISpecificationCalculationAnalysis _analysis;
        private readonly IReIndexGraphRepository _reIndexGraphs;

        private const string SpecificationId = "specification-id";

        public ReIndexSpecificationCalculationRelationships(ISpecificationCalculationAnalysis analysis,
            IReIndexGraphRepository reIndexGraphs,
            ILogger logger,
            IJobManagement jobManagement) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(analysis, nameof(analysis));
            Guard.ArgumentNotNull(reIndexGraphs, nameof(reIndexGraphs));
            
            _analysis = analysis;
            _reIndexGraphs = reIndexGraphs;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = message.GetUserProperty<string>(SpecificationId);

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            SpecificationCalculationRelationships specificationCalculationRelationships = await _analysis
                    .GetSpecificationCalculationRelationships(specificationId);

            SpecificationCalculationRelationships specificationCalculationUnusedRelationships = await _reIndexGraphs.GetUnusedRelationships(specificationCalculationRelationships);

            await _reIndexGraphs.RecreateGraph(specificationCalculationRelationships, specificationCalculationUnusedRelationships);
        }
    }
}