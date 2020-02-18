using System;
using System.Threading.Tasks;
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
        private readonly Interfaces.Analysis.IGraphRepository _graphs;
        private readonly ILogger _logger;

        private const string SpecificationId = "specification-id";

        public ReIndexSpecificationCalculationRelationships(ISpecificationCalculationAnalysis analysis,
            Interfaces.Analysis.IGraphRepository graphs,
            ILogger logger)
        {
            Guard.ArgumentNotNull(analysis, nameof(analysis));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(graphs, nameof(graphs));            
            
            _analysis = analysis;
            _logger = logger;
            _graphs = graphs;
        }

        public async Task Run(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = message.GetUserProperty<string>(SpecificationId);
            
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            try
            {
                SpecificationCalculationRelationships specificationCalculationRelationships = await _analysis
                    .GetSpecificationCalculationRelationships(specificationId);

                await _graphs.RecreateGraph(specificationCalculationRelationships);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to reindex specification calculation relationships");
                
                throw;
            }
        }
    }
}