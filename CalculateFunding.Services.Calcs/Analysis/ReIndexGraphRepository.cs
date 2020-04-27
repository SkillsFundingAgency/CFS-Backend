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
using ApiDataField = CalculateFunding.Common.ApiClient.Graph.Models.DataField;
using ApiDataset = CalculateFunding.Common.ApiClient.Graph.Models.Dataset;
using ApiDatasetDefinition = CalculateFunding.Common.ApiClient.Graph.Models.DatasetDefinition;
using ApiEntitySpecification = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Specification>;
using ApiEntityCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation>;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Models.Graph;
using CalculateFunding.Common.Extensions;

namespace CalculateFunding.Services.Calcs.Analysis
{
    public class ReIndexGraphRepository : IReIndexGraphRepository
    {
        private readonly IGraphApiClient _graphApiClient;
        private readonly IMapper _mapper;
        private readonly AsyncPolicy _resilience;
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
            ApiResponse<IEnumerable<ApiEntitySpecification>> apiResponse = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.GetAllEntitiesRelatedToSpecification(specificationCalculationRelationships.Specification.SpecificationId));

            IEnumerable<ApiEntitySpecification> entities = apiResponse?.Content;

            IEnumerable<Calculation> removedSpecificationCalculations = RemoveCalculationSpecificationRelationships(specificationCalculationRelationships.Calculations, entities);
            IEnumerable<CalculationRelationship> removedCalculationRelationships = RemoveCalculationCalculationRelationships(specificationCalculationRelationships.CalculationRelationships, entities);
            IEnumerable<CalculationDataFieldRelationship> removedDatasetReferences = await RemoveDataFieldCalculationRelationships(specificationCalculationRelationships.CalculationDataFieldRelationships, specificationCalculationRelationships.Calculations);

            return new SpecificationCalculationRelationships
            {
                Specification = specificationCalculationRelationships.Specification,
                Calculations = removedSpecificationCalculations,
                CalculationRelationships = removedCalculationRelationships,
                CalculationDataFieldRelationships = removedDatasetReferences
            };
        }

        private IEnumerable<Calculation> RemoveCalculationSpecificationRelationships(IEnumerable<Calculation> calculations, IEnumerable<ApiEntitySpecification> entities)
        {
            // retrieve all calculations from the current graph related to the specification
            IEnumerable<Calculation> specificationCalculations = entities.SelectMany(_ =>
                _.Relationships.Where(rel => rel.Type == "BelongsToSpecification")
                .Select(calc => ((object)calc.One).AsJson().AsPoco<Calculation>())).Distinct();

            // retrieve all the new calculations related to the specification
            HashSet<string> newSpecificationCalculations = calculations.Select(_ => _.CalculationId).ToHashSet();

            // retrieve all calculations which exist in the current graph but not in the new result set
            IEnumerable<Calculation> removedSpecificationCalculations = specificationCalculations
                .Where(_ => !newSpecificationCalculations.Contains(_.CalculationId));

            return removedSpecificationCalculations;
        }

        private async Task<IEnumerable<CalculationDataFieldRelationship>> RemoveDataFieldCalculationRelationships(IEnumerable<CalculationDataFieldRelationship> datasetReferences, IEnumerable<Calculation> calculations)
        {
            IEnumerable<CalculationDataFieldRelationship> calculationDatasetReferencesList = new CalculationDataFieldRelationship[0];

            foreach (Calculation calculation in calculations)
            {
                ApiResponse<IEnumerable<ApiEntityCalculation>> apiResponse = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.GetAllEntitiesRelatedToCalculation(calculation.CalculationId));

                IEnumerable<ApiEntityCalculation> entities = apiResponse?.Content;

                if (entities != null)
                {
                    calculationDatasetReferencesList = calculationDatasetReferencesList.Concat(entities.SelectMany(_ =>
                    _.Relationships.Where(rel => rel.Type == "ReferencesDataField")
                    .Select(datasetRelationship => new CalculationDataFieldRelationship
                    {
                        Calculation = ((object)datasetRelationship.One).AsJson().AsPoco<Calculation>(),
                        DataField = ((object)datasetRelationship.Two).AsJson().AsPoco<DataField>()
                    })));
                }
            }

            // retrieve all dataset referenced from the current graph related to the calculationc
            IDictionary<string, CalculationDataFieldRelationship> calculationDatasetReferences = calculationDatasetReferencesList.ToDictionary(_ => $"{_.Calculation.CalculationId}{_.DataField.DataFieldId}");

            // retrieve all the new dataset fields related to the calculations
            IDictionary<string, CalculationDataFieldRelationship> newCalculationDatafields = datasetReferences.ToDictionary(_ => $"{_.Calculation.CalculationId}{_.DataField.DataFieldId}");

            // retrieve all dataset field relationships which exist in the current graph but not in the new result set
            IEnumerable<CalculationDataFieldRelationship> removedDatasetFields = calculationDatasetReferences
                .Where(_ => !newCalculationDatafields.ContainsKey(_.Key)).Select(_ => _.Value);

            return removedDatasetFields;
        }

        private IEnumerable<CalculationRelationship> RemoveCalculationCalculationRelationships(IEnumerable<CalculationRelationship> calculationRelationships, IEnumerable<ApiEntitySpecification> entities)
        {
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
            IDictionary<string, CalculationRelationship> calculationRelationshipsDictionary = calculationRelationships.ToDictionary(_ => $"{_.CalculationOneId}{_.CalculationTwoId}");

            // retrieve all calculation to calculation relationships from current graph not in the new result set
            IEnumerable<CalculationRelationship> removedCalculationRelationships = calculationCalculationRelations
                .Where(_ => !calculationRelationshipsDictionary.ContainsKey(_.Key)).Select(_ => _.Value);

            return removedCalculationRelationships;
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

            await DeleteDatasetRelationships(specificationCalculationUnusedRelationships.CalculationDataFieldRelationships);
        }

        private async Task InsertSpecificationGraph(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            await InsertSpecificationNode(specificationCalculationRelationships.Specification);
            await InsertCalculationNodes(specificationCalculationRelationships.Calculations);
            await InsertSpecificationRelationships(specificationCalculationRelationships);
            await InsertCalculationRelationships(specificationCalculationRelationships.CalculationRelationships);
            await InsertDataFieldRelationships(specificationCalculationRelationships.CalculationDataFieldRelationships);
            await InsertDatasetDataFieldRelationships(specificationCalculationRelationships.DatasetDataFieldRelationships, specificationCalculationRelationships.Specification.SpecificationId);
            await InsertDatasetDatasetDefinitionRelationships(specificationCalculationRelationships.DatasetDatasetDefinitionRelationships);
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

        private async Task DeleteDatasetRelationships(IEnumerable<CalculationDataFieldRelationship> datasetRelationships)
        {
            foreach (CalculationDataFieldRelationship datasetRelationship in datasetRelationships)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() => _graphApiClient.DeleteCalculationDataFieldRelationship(datasetRelationship.Calculation.CalculationId, datasetRelationship.DataField.DataFieldId));

                EnsureApiCallSucceeded(response, $"Unable to delete previous graph relationship for calculation id {datasetRelationship.Calculation.CalculationId} and data field id {datasetRelationship.DataField.DataFieldId}");
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

        private async Task InsertDataFieldRelationships(IEnumerable<CalculationDataFieldRelationship> datasetRelationships)
        {
            IEnumerable<IGrouping<string, CalculationDataFieldRelationship>> relationshipsPerCalculation =
                datasetRelationships.GroupBy(_ => _.Calculation.CalculationId);

            foreach (IGrouping<string, CalculationDataFieldRelationship> relationships in relationshipsPerCalculation)
            {
                HttpStatusCode response;

                ApiDataField[] apiDataFields = relationships.Select(_ => _mapper.Map<ApiDataField>(_.DataField)).Distinct().ToArray();

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertDataFields(apiDataFields));

                EnsureApiCallSucceeded(response, "Unable to create data field");

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertCalculationDataFieldsRelationships(relationships.Key,
                        relationships.Select(_ => _.DataField.DataFieldId).ToArray()));

                EnsureApiCallSucceeded(response, "Unable to create data field calculation relationships");
            }
        }

        private async Task InsertDatasetDataFieldRelationships(IEnumerable<DatasetDataFieldRelationship> datasetDataFieldRelationships, string specificationId)
        {
            IEnumerable<IGrouping<string, DatasetDataFieldRelationship>> relationshipsPerDataset =
                datasetDataFieldRelationships.GroupBy(_ => _.Dataset.DatasetId);

            foreach (IGrouping<string, DatasetDataFieldRelationship> relationships in relationshipsPerDataset)
            {
                HttpStatusCode response;

                ApiDataset apiDataset = relationships.Select(_ => _mapper.Map<ApiDataset>(_.Dataset)).Distinct().First();

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertDataset(apiDataset));

                EnsureApiCallSucceeded(response, "Unable to create dataset");

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertSpecificationDatasetRelationship(specificationId, relationships.Key));

                EnsureApiCallSucceeded(response, "Unable to create specification dataset relationship");

                foreach (string fieldid in relationships.Select(_ => _.DataField.DataFieldId))
                {
                    response = await _resilience.ExecuteAsync(() =>
                        _graphApiClient.UpsertDatasetDataFieldRelationship(relationships.Key,
                            fieldid));
                }

                EnsureApiCallSucceeded(response, "Unable to create dataset data field relationships");
            }
        }

        private async Task InsertDatasetDatasetDefinitionRelationships(IEnumerable<DatasetDatasetDefinitionRelationship> datasetDatasetDefinitionRelationships)
        {
            IEnumerable<IGrouping<string, DatasetDatasetDefinitionRelationship>> relationshipsPerDataset =
                datasetDatasetDefinitionRelationships.GroupBy(_ => _.Dataset.DatasetId);

            foreach (IGrouping<string, DatasetDatasetDefinitionRelationship> relationships in relationshipsPerDataset)
            {
                HttpStatusCode response;

                ApiDatasetDefinition[] apiDatasetDefinitions = relationships.Select(_ => _mapper.Map<ApiDatasetDefinition>(_.DatasetDefinition)).Distinct().ToArray();

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertDatasetDefinitions(apiDatasetDefinitions));

                EnsureApiCallSucceeded(response, "Unable to create dataset definitions");

                foreach (string definitionid in relationships.Select(_ => _.DatasetDefinition.DatasetDefinitionId))
                {
                    response = await _resilience.ExecuteAsync(() =>
                        _graphApiClient.UpsertDataDefinitionDatasetRelationship(definitionid, 
                            relationships.Key));
                }

                EnsureApiCallSucceeded(response, "Unable to create dataset dataset definition relationships");
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