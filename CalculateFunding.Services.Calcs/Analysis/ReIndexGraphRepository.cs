using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using Polly;
using Serilog;
using ApiSpecification = CalculateFunding.Common.ApiClient.Graph.Models.Specification;
using ApiCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using ApiEntity = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Specification>;
using ApiRelationship = CalculateFunding.Common.ApiClient.Graph.Models.Relationship;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Models.Graph;
using CalculateFunding.Common.Extensions;

namespace CalculateFunding.Services.Calcs.Analysis
{
    public class ReIndexGraphRepository : IReIndexGraphRepository
    {
        private readonly IGraphApiClient _graphApiClient;
        private readonly IMapper _mapper;
        private readonly Policy _resilience;
        private readonly ILogger _logger;

        public ReIndexGraphRepository(IGraphApiClient graphApiClient,
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

        public async Task<SpecificationCalculationRelationships> GetUnusedRelationships(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            ApiResponse<IEnumerable<ApiEntity>> apiResponse = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.GetAllEntities(specificationCalculationRelationships.Specification.SpecificationId));

            IEnumerable<ApiEntity> entities = apiResponse?.Content;

            // retrieve all the new calculations related to the specification
            HashSet<string> newSpecificationCalculations = specificationCalculationRelationships.Calculations.Select(_ => _.CalculationId).ToHashSet();

            // retrieve all calculations from the current graph related to the specification
            IEnumerable<Calculation> specificationCalculations = entities.SelectMany(_ =>
                _.Relationships.Where(rel => rel.Type == "BelongsToSpecification")
                .Select(calc => ((object)calc.One).AsJson().AsPoco<Calculation>())).Distinct();

            // retrieve all calculations which exist in the current graph but not in the new result set
            IEnumerable<Calculation> removedSpecificationCalculations = specificationCalculations
                .Where(_ => !newSpecificationCalculations.Contains(_.CalculationId));

            // retrieve all calculation to calculation relationships from current graph
            IDictionary<string, CalculationRelationship> calculationCalculationRelations = entities.SelectMany(_ =>
                _.Relationships.Where(rel =>
                    rel.Type == "CallsCalculation")).Select(_ => 
                    new CalculationRelationship
                    {
                        CalculationOneId = ((object)_.One).AsJson().AsPoco<Calculation>().CalculationId,
                        CalculationTwoId = ((object)_.Two).AsJson().AsPoco<Calculation>().CalculationId
                    }).Distinct().ToDictionary(_ => $"{_.CalculationOneId}{_.CalculationTwoId}");

            // retrieve all new calculation relationships 
            IDictionary<string, CalculationRelationship> specificationCalculationRelationshipsDictionary = specificationCalculationRelationships.CalculationRelationships.ToDictionary(_ => $"{_.CalculationOneId}{_.CalculationTwoId}");

            // retrieve all calculation to calculation relationships from current graph not in the new result set
            IEnumerable<CalculationRelationship> removedCalculationRelationships = calculationCalculationRelations
                .Where(_ => !specificationCalculationRelationshipsDictionary.ContainsKey(_.Key)).Select(_ => _.Value);

            return new SpecificationCalculationRelationships
            {
                Specification = specificationCalculationRelationships.Specification,
                Calculations = removedSpecificationCalculations,
                CalculationRelationships = removedCalculationRelationships
            };
        }

        public async Task RecreateGraph(SpecificationCalculationRelationships specificationCalculationRelationships, SpecificationCalculationRelationships specificationCalculationUnusedRelationships)
        {
            try
            {
                Guard.ArgumentNotNull(specificationCalculationRelationships, nameof(specificationCalculationRelationships));
                Guard.ArgumentNotNull(specificationCalculationUnusedRelationships, nameof(specificationCalculationUnusedRelationships));

                await DeleteSpecificationRelationshipGraph(specificationCalculationUnusedRelationships);
                await InsertSpecificationGraph(specificationCalculationRelationships);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to complete calculation relationship upserts");

                throw;
            }
        }

        private async Task DeleteSpecificationRelationshipGraph(SpecificationCalculationRelationships specificationCalculationUnusedRelationships)
        {
            Specification specification = specificationCalculationUnusedRelationships.Specification;

            Guard.ArgumentNotNull(specification, nameof(specification));

            await DeleteSpecificationCalculationRelationships(specification, 
                specificationCalculationUnusedRelationships.Calculations);

            await DeleteCalculationRelationships(specificationCalculationUnusedRelationships.CalculationRelationships);
        }

        private async Task InsertSpecificationGraph(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            await InsertSpecificationNode(specificationCalculationRelationships.Specification);
            await InsertCalculationNodes(specificationCalculationRelationships.Calculations);
            await InsertSpecificationRelationships(specificationCalculationRelationships);
            await InsertCalculationRelationships(specificationCalculationRelationships.CalculationRelationships);
        }

        private async Task DeleteSpecificationCalculationRelationships(Specification specification, IEnumerable<Calculation> calculations)
        {
            string specificationId = specification?.SpecificationId;

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            foreach (Calculation calculation in calculations)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() => _graphApiClient.DeleteCalculationSpecificationRelationship(calculation.CalculationId, specificationId));

                EnsureApiCallSucceeded(response, $"Unable to delete previous graph relationship for specification id {specificationId} and calculation id {calculation.CalculationId}");
            }
        }

        private async Task DeleteCalculationRelationships(IEnumerable<CalculationRelationship> calculationRelationships)
        {
            foreach (CalculationRelationship calculationRelationship in calculationRelationships)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() => _graphApiClient.DeleteCalculationCalculationRelationship(calculationRelationship.CalculationOneId, calculationRelationship.CalculationTwoId));

                EnsureApiCallSucceeded(response, $"Unable to delete previous graph relationship for calculation id {calculationRelationship.CalculationOneId} and calculation id {calculationRelationship.CalculationTwoId}");
            }
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

            ApiCalculation[] graphCalculations = _mapper.Map<IEnumerable<ApiCalculation>>(calculations)
                .ToArray();

            HttpStatusCode response = await _resilience.ExecuteAsync(() => _graphApiClient.UpsertCalculations(graphCalculations));

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