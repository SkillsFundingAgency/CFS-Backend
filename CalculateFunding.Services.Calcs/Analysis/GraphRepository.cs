using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Calcs.Interfaces;
using Polly;
using Serilog;
using ApiSpecification = CalculateFunding.Common.ApiClient.Graph.Models.Specification;
using ApiCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;

namespace CalculateFunding.Services.Calcs.Analysis
{
    public class GraphRepository : IGraphRepository
    {
        private readonly IGraphApiClient _graphApiClient;
        private readonly IMapper _mapper;
        private readonly Policy _resilience;
        private readonly ILogger _logger;

        public GraphRepository(IGraphApiClient graphApiClient,
            ICalcsResiliencePolicies resiliencePolicies,
            IMapper mapper,
            ILogger logger)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(graphApiClient, nameof(graphApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.GraphApiClientPolicy, nameof(resiliencePolicies.GraphApiClientPolicy));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _graphApiClient = graphApiClient;
            _logger = logger;
            _mapper = mapper;
            _resilience = resiliencePolicies.GraphApiClientPolicy;
        }

        public async Task RecreateGraph(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            try
            {
                Guard.ArgumentNotNull(specificationCalculationRelationships, nameof(specificationCalculationRelationships));

                await DeleteSpecificationGraph(specificationCalculationRelationships);
                await InsertSpecificationGraph(specificationCalculationRelationships);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to complete calculation relationship upserts");

                throw;
            }
        }

        private async Task DeleteSpecificationGraph(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            Specification specification = specificationCalculationRelationships.Specification;

            Guard.ArgumentNotNull(specification, nameof(specification));

            await DeleteCalculationRelationshipsForSpecification(specification);
        }

        private async Task InsertSpecificationGraph(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            await InsertSpecificationNode(specificationCalculationRelationships.Specification);
            await InsertCalculationNodes(specificationCalculationRelationships.Calculations);
            await InsertSpecificationRelationships(specificationCalculationRelationships);
            await InsertCalculationRelationships(specificationCalculationRelationships.CalculationRelationships);
        }

        private async Task DeleteCalculationRelationshipsForSpecification(Specification specification)
        {
            string specificationId = specification?.SpecificationId;

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            HttpStatusCode response = await _resilience.ExecuteAsync(() => _graphApiClient.DeleteAllForSpecification(specificationId));

            EnsureApiCallSucceeded(response, $"Unable to delete previous graph for specification id {specificationId}");
        }

        private async Task InsertSpecificationNode(Specification specification)
        {
            Guard.ArgumentNotNull(specification, nameof(specification));

            ApiSpecification graphSpecification = _mapper.Map<ApiSpecification>(specification);

            HttpStatusCode response = await _resilience.ExecuteAsync(() =>
                _graphApiClient.UpsertSpecifications(new[] {graphSpecification}));

            EnsureApiCallSucceeded(response, "Unable to create specification node");
        }

        private async Task InsertCalculationNodes(IEnumerable<Calculation> calculations)
        {
            Guard.IsNotEmpty(calculations, nameof(calculations));

            IEnumerable<ApiCalculation> graphCalculations = _mapper.Map<IEnumerable<ApiCalculation>>(calculations);

            HttpStatusCode response = await _resilience.ExecuteAsync(() => _graphApiClient.UpsertCalculations(graphCalculations.ToArray()));

            EnsureApiCallSucceeded(response, "Unable to create calculation nodes");
        }

        private async Task InsertSpecificationRelationships(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            string specificationId = specificationCalculationRelationships.Specification.SpecificationId;
            IEnumerable<string> calculationIds = specificationCalculationRelationships.Calculations.Select(_ => _.CalculationId);
            
            foreach (string calculationId in calculationIds)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() => 
                    _graphApiClient.UpsertCalculationSpecificationRelationship(calculationId, specificationId));
                
                EnsureApiCallSucceeded(response, $"Unable to create specification calculation relationship {specificationId}->{calculationId}");
            }
        }

        private async Task InsertCalculationRelationships(IEnumerable<CalculationRelationship> calculationRelationships)
        {
            Guard.IsNotEmpty(calculationRelationships, nameof(calculationRelationships));

            IEnumerable<IGrouping<string, CalculationRelationship>> relationshipsPerCalculation =
                calculationRelationships.GroupBy(_ => _.CalculationOneId);

            foreach (IGrouping<string, CalculationRelationship> relationships in relationshipsPerCalculation)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertCalculationCalculationsRelationships(relationships.Key,
                        relationships.Select(_ => _.CalculationTwoId).ToArray()));

                EnsureApiCallSucceeded(response, "Unable to create calculation relationships");
            }
        }

        private void EnsureApiCallSucceeded(HttpStatusCode statusCode, string errorMessage)
        {
            if (statusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}