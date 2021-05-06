using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Threading;
using Polly;
using Serilog;
using ApiSpecification = CalculateFunding.Common.ApiClient.Graph.Models.Specification;
using ApiCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using ApiEnum = CalculateFunding.Common.ApiClient.Graph.Models.Enum;
using ApiDataField = CalculateFunding.Common.ApiClient.Graph.Models.DataField;
using ApiDataset = CalculateFunding.Common.ApiClient.Graph.Models.Dataset;
using ApiDatasetDefinition = CalculateFunding.Common.ApiClient.Graph.Models.DatasetDefinition;
using ApiFundingLine = CalculateFunding.Common.ApiClient.Graph.Models.FundingLine;
using ApiEntitySpecification = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Specification>;
using ApiEntityCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation>;
using ApiEntityFundingLine = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.FundingLine>;
using ApiRelationship = CalculateFunding.Common.ApiClient.Graph.Models.Relationship;
using Calculation = CalculateFunding.Models.Graph.Calculation;
using DataField = CalculateFunding.Models.Graph.DataField;
using FundingLine = CalculateFunding.Models.Graph.FundingLine;
using IEnumerableExtensions = CalculateFunding.Common.Extensions.IEnumerableExtensions;
using Specification = CalculateFunding.Models.Graph.Specification;
using Enum = CalculateFunding.Models.Graph.Enum;

namespace CalculateFunding.Services.Calcs.Analysis
{
    using static IEnumerableExtensions;

    public class ReIndexGraphRepository : IReIndexGraphRepository
    {
        private const int PageSize = 100;
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

            if (entities.IsNullOrEmpty())
            {
                return null;
            }

            IEnumerable<ApiEntityCalculation> allCalculationsRelatedToSpecification = await GetAllCalculationsRelatedToSpecification(specificationCalculationRelationships);
            
            IEnumerable<Calculation> removedSpecificationCalculations = RemoveCalculationSpecificationRelationships(specificationCalculationRelationships.Calculations, entities);
            IEnumerable<CalculationRelationship> removedCalculationRelationships =
                RemoveCalculationCalculationRelationships(specificationCalculationRelationships.CalculationRelationships, allCalculationsRelatedToSpecification);
            IEnumerable<CalculationDataFieldRelationship> removedDatasetReferences =
                await RemoveDataFieldCalculationRelationships(specificationCalculationRelationships.CalculationDataFieldRelationships, allCalculationsRelatedToSpecification);
            IEnumerable<CalculationEnumRelationship> removedCalculationEnumRelationship =
                await RemoveCalculationEnumRelationships(specificationCalculationRelationships.CalculationEnumRelationships, allCalculationsRelatedToSpecification);
            IEnumerable<FundingLine> removedFundingLines = await RemoveFundingLines(specificationCalculationRelationships.FundingLineRelationships, specificationCalculationRelationships.FundingLines);

            return new SpecificationCalculationRelationships
            {
                Specification = specificationCalculationRelationships.Specification,
                Calculations = removedSpecificationCalculations,
                FundingLines = removedFundingLines,
                CalculationRelationships = removedCalculationRelationships,
                CalculationDataFieldRelationships = removedDatasetReferences,
                CalculationEnumRelationships = removedCalculationEnumRelationship
            };
        }

        private async Task<IEnumerable<ApiEntityCalculation>> GetAllCalculationsRelatedToSpecification(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            PagedContext<Calculation> pagedRequests = new PagedContext<Calculation>(specificationCalculationRelationships.Calculations,
                PageSize);

            List<ApiEntityCalculation> allCalculations = new List<ApiEntityCalculation>();

            while (pagedRequests.HasPages)
            {
                ApiResponse<IEnumerable<ApiEntityCalculation>> apiCalcResponse = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.GetAllEntitiesRelatedToCalculations(pagedRequests
                        .NextPage()
                        .Select(_ => _.CalculationId)
                        .ToArray()));

                IEnumerable<ApiEntityCalculation> calculationsPage = apiCalcResponse?.Content;

                if (calculationsPage == null || !calculationsPage.Any())
                {
                    continue;
                }

                allCalculations.AddRange(calculationsPage);
            }

            return allCalculations;
        }

        private IEnumerable<Calculation> RemoveCalculationSpecificationRelationships(IEnumerable<Calculation> calculations,
            IEnumerable<ApiEntitySpecification> entities)
        {
            // retrieve all calculation to specification relationships from current graph
            IEnumerable<ApiRelationship> specificationCalculations = entities?.Where(_ => _.Relationships != null).SelectMany(_ =>
                _.Relationships.Where(rel =>
                    rel.Type.Equals(SpecificationCalculationRelationships.FromIdField, StringComparison.InvariantCultureIgnoreCase)));

            // if there are no calculation relationships to remove then exit early
            if (specificationCalculations.IsNullOrEmpty())
            {
                return null;
            }

            // retrieve all the new calculations related to the specification
            HashSet<string> newSpecificationCalculations = calculations.Select(_ => _.CalculationId).ToHashSet();

            // retrieve all calculations which exist in the current graph but not in the new result set
            IEnumerable<Calculation> removedSpecificationCalculations = specificationCalculations
                .Select(calc => ((object) calc.One).AsJson().AsPoco<Calculation>())
                .Where(_ => !newSpecificationCalculations.Contains(_.CalculationId));

            return removedSpecificationCalculations;
        }

        private async Task<IEnumerable<FundingLine>> RemoveFundingLines(IEnumerable<FundingLineCalculationRelationship> fundingLineCalculationRelationships,
            IEnumerable<FundingLine> allFundingLines)
        {
            List<FundingLine> fundingLines = new List<FundingLine>();

            // there are no cash funding lines
            if (allFundingLines.IsNullOrEmpty())
            {
                return null;
            }

            PagedContext<FundingLine> pagedRequests = new PagedContext<FundingLine>(allFundingLines, PageSize);

            while (pagedRequests.HasPages)
            {
                ApiResponse<IEnumerable<ApiEntityFundingLine>> apiResponse = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.GetAllEntitiesRelatedToFundingLines(pagedRequests
                        .NextPage()
                        .Select(_ => _.SpecificationFundingLineId)
                        .ToArray()));
                
                IEnumerable<ApiEntityFundingLine> entities = apiResponse?.Content;
                
                if (entities == null || !entities.Any())
                {
                    continue;
                }
                
                fundingLines.AddRange(entities.Select(_ => _mapper.Map<FundingLine>(_.Node)));
            }

            // no funding lines exist in the current graph
            if (fundingLines.IsNullOrEmpty())
            {
                return null;
            }

            // there are no funding lines in the new set of funding lines so need to delete all current funding lines
            if (fundingLineCalculationRelationships.IsNullOrEmpty())
            {
                return fundingLines;
            }

            return fundingLines.Where(_ => !fundingLineCalculationRelationships.Any(rel => rel.FundingLine.SpecificationFundingLineId == _.SpecificationFundingLineId));
        }
        
        private async Task<IEnumerable<CalculationEnumRelationship>> RemoveCalculationEnumRelationships(IEnumerable<CalculationEnumRelationship> enumReferences,
            IEnumerable<ApiEntityCalculation> calculations)
        {
            // retrieve all calculation to enum relationships from current graph
            IEnumerable<ApiRelationship> calculationEnumRelations = calculations?.Where(_ => _.Relationships != null).SelectMany(_ =>
                _.Relationships.Where(rel =>
                    rel.Type.Equals(CalculationEnumRelationship.FromIdField, StringComparison.InvariantCultureIgnoreCase)));

            // if there are no calculation relationships to remove then exit early
            if (calculationEnumRelations.IsNullOrEmpty())
            {
                return null;
            }

            // retrieve all emums referenced from the current graph related to the calculations
            IDictionary<string, CalculationEnumRelationship> calculationEnumReferences = calculationEnumRelations.Select(_ =>
                new CalculationEnumRelationship
                {
                    //double check how we handle dynamic at the other end as I don't believe this is needed
                    Calculation = ((object)_.One).AsJson().AsPoco<Calculation>(),
                    Enum = ((object)_.Two).AsJson().AsPoco<Enum>()
                }).DistinctBy(_ => new {_.Calculation.CalculationId, _.Enum.EnumId}).ToDictionary(_ => $"{_.Calculation.CalculationId}{_.Enum.EnumId}");

            // there are no enum references in the new set of calculations so need to delete all current references
            if (enumReferences.IsNullOrEmpty())
            {
                return calculationEnumReferences.Values;
            }

            // retrieve all the new enums related to the calculations
            IDictionary<string, CalculationEnumRelationship> newCalculationEnums = enumReferences.DistinctBy(_ => new {_.Calculation.CalculationId, _.Enum.EnumId}).ToDictionary(_ => $"{_.Calculation.CalculationId}{_.Enum.EnumId}");

            // retrieve all dataset field relationships which exist in the current graph but not in the new result set
            IEnumerable<CalculationEnumRelationship> removedEnums = calculationEnumReferences
                .Where(_ => !newCalculationEnums.ContainsKey(_.Key)).Select(_ => _.Value);

            return removedEnums;
        }

        private async Task<IEnumerable<CalculationDataFieldRelationship>> RemoveDataFieldCalculationRelationships(IEnumerable<CalculationDataFieldRelationship> datasetReferences,
            IEnumerable<ApiEntityCalculation> calculations)
        {
            // retrieve all calculation to datafield relationships from current graph
            IEnumerable<ApiRelationship> calculationDataFieldRelations = calculations?.Where(_ => _.Relationships != null).SelectMany(_ =>
                _.Relationships.Where(rel =>
                    rel.Type.Equals(CalculationDataFieldRelationship.FromIdField, StringComparison.InvariantCultureIgnoreCase)));

            // if there are no calculation relationships to remove then exit early
            if (calculationDataFieldRelations.IsNullOrEmpty())
            {
                return null;
            }

            // retrieve all dataset referenced from the current graph related to the calculations
            IDictionary<string, CalculationDataFieldRelationship> calculationDatasetReferences = calculationDataFieldRelations.Select(_ =>
                new CalculationDataFieldRelationship
                {
                    //double check how we handle dynamic at the other end as I don't believe this is needed
                    Calculation = ((object)_.One).AsJson().AsPoco<Calculation>(),
                    DataField = ((object)_.Two).AsJson().AsPoco<DataField>()
                }).DistinctBy(_ => new {_.Calculation.CalculationId, _.DataField.DataFieldId}).ToDictionary(_ => $"{_.Calculation.CalculationId}{_.DataField.DataFieldId}");

            // there are no dataset references in the new set of calculations so need to delete all current references
            if (datasetReferences.IsNullOrEmpty())
            {
                return calculationDatasetReferences.Values;
            }

            // retrieve all the new dataset fields related to the calculations
            IDictionary<string, CalculationDataFieldRelationship> newCalculationDatafields = datasetReferences.DistinctBy(_ => new {_.Calculation.CalculationId, _.DataField.DataFieldId}).ToDictionary(_ => $"{_.Calculation.CalculationId}{_.DataField.DataFieldId}");

            // retrieve all dataset field relationships which exist in the current graph but not in the new result set
            IEnumerable<CalculationDataFieldRelationship> removedDatasetFields = calculationDatasetReferences
                .Where(_ => !newCalculationDatafields.ContainsKey(_.Key)).Select(_ => _.Value);

            return removedDatasetFields;
        }

        private IEnumerable<CalculationRelationship> RemoveCalculationCalculationRelationships(IEnumerable<CalculationRelationship> calculationRelationships,
            IEnumerable<ApiEntityCalculation> calculations)
        {
            // retrieve all calculation to calculation relationships from current graph
            IEnumerable<ApiRelationship> calculationCalculationRelations = calculations?.Where(_ => _.Relationships != null).SelectMany(_ =>
                _.Relationships.Where(rel =>
                    rel.Type.Equals(CalculationRelationship.ToIdField, StringComparison.InvariantCultureIgnoreCase)));

            // if there are no calculation relationships to remove then exit early
            if (calculationCalculationRelations.IsNullOrEmpty())
            {
                return null;
            }

            IDictionary<string, CalculationRelationship> calculationCalculationRelationsDictionary = calculationCalculationRelations.Select(_ =>
                new CalculationRelationship
                {
                    //double check how we handle dynamic at the other end as I don't believe this is needed
                    CalculationOneId = ((object) _.One).AsJson().AsPoco<Calculation>().CalculationId,
                    CalculationTwoId = ((object) _.Two).AsJson().AsPoco<Calculation>().CalculationId
                }).DistinctBy(_ => new {_.CalculationOneId, _.CalculationTwoId}).ToDictionary(_ => $"{_.CalculationOneId}{_.CalculationTwoId}");

            // retrieve all new calculation relationships 
            IDictionary<string, CalculationRelationship> calculationRelationshipsDictionary = calculationRelationships.DistinctBy(_ => new {_.CalculationOneId, _.CalculationTwoId}).ToDictionary(_ => $"{_.CalculationOneId}{_.CalculationTwoId}");

            // retrieve all calculation to calculation relationships from current graph not in the new result set
            IEnumerable<CalculationRelationship> removedCalculationRelationships = calculationCalculationRelationsDictionary
                .Where(_ => !calculationRelationshipsDictionary.ContainsKey(_.Key)).Select(_ => _.Value);

            return removedCalculationRelationships;
        }

        public async Task RecreateGraph(SpecificationCalculationRelationships specificationCalculationRelationships,
            SpecificationCalculationRelationships specificationCalculationUnusedRelationships)
        {
            try
            {
                Guard.ArgumentNotNull(specificationCalculationRelationships, nameof(specificationCalculationRelationships));

                if (specificationCalculationUnusedRelationships != null)
                {
                    await DeleteSpecificationRelationshipGraph(specificationCalculationUnusedRelationships);
                }

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

            if (specificationCalculationUnusedRelationships.Calculations.AnyWithNullCheck())
            {
                await DeleteSpecificationCalculationRelationships(specification,
                    specificationCalculationUnusedRelationships.Calculations);
            }

            if (specificationCalculationUnusedRelationships.FundingLines.AnyWithNullCheck())
            {
                await DeleteFundingLines(specificationCalculationUnusedRelationships.FundingLines);
            }

            if (specificationCalculationUnusedRelationships.CalculationRelationships.AnyWithNullCheck())
            {
                await DeleteCalculationRelationships(specificationCalculationUnusedRelationships.CalculationRelationships);
            }

            if (specificationCalculationUnusedRelationships.CalculationDataFieldRelationships.AnyWithNullCheck())
            {
                await DeleteDatasetRelationships(specificationCalculationUnusedRelationships.CalculationDataFieldRelationships);
            }

            if (specificationCalculationUnusedRelationships.CalculationEnumRelationships.AnyWithNullCheck())
            {
                await DeleteEnumRelationships(specificationCalculationUnusedRelationships.CalculationEnumRelationships);
            }
        }

        private async Task InsertSpecificationGraph(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            Task[] graphTasks =
            {
                UpsertSpecificationNode(specificationCalculationRelationships.Specification),
                UpsertCalculationNodes(specificationCalculationRelationships.Calculations)
            };

            await TaskHelper.WhenAllAndThrow(graphTasks);

            Task[] subGraphTasks =
            {
                UpsertSpecificationRelationships(specificationCalculationRelationships),
                UpsertCalculationRelationships(specificationCalculationRelationships.CalculationRelationships),
                UpsertDataFieldRelationships(specificationCalculationRelationships.CalculationDataFieldRelationships),
                UpsertEnumRelationships(specificationCalculationRelationships.CalculationEnumRelationships),
                UpsertDatasetDataFieldRelationships(specificationCalculationRelationships.DatasetDataFieldRelationships, specificationCalculationRelationships.Specification.SpecificationId),
                UpsertDatasetDatasetDefinitionRelationships(specificationCalculationRelationships.DatasetDatasetDefinitionRelationships)
            };

            await TaskHelper.WhenAllAndThrow(subGraphTasks);

            if (specificationCalculationRelationships.FundingLineRelationships?.Any() == true)
            {
                await UpsertFundingLines(specificationCalculationRelationships.FundingLineRelationships);
            }
        }

        private async Task DeleteSpecificationCalculationRelationships(Specification specification,
            IEnumerable<Calculation> calculations)
        {
            string specificationId = specification?.SpecificationId;

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            PagedContext<AmendRelationshipRequestModel> pagedRequests = new PagedContext<AmendRelationshipRequestModel>(calculations
                    .Select(_ => new AmendRelationshipRequestModel
                    {
                        IdA = _.CalculationId,
                        IdB = specificationId
                    }),
                PageSize);

            while (pagedRequests.HasPages)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() => _graphApiClient.DeleteCalculationSpecificationRelationships(pagedRequests
                    .NextPage()
                    .ToArray()));

                EnsureApiCallSucceeded(response, $"Unable to delete previous graph relationship for specification id {specificationId} and calculation ids");
            }
        }

        private async Task DeleteFundingLines(IEnumerable<FundingLine> fundingLines)
        {
            PagedContext<string> pagedRequests = new PagedContext<string>(fundingLines.Select(_ => _.SpecificationFundingLineId), PageSize);

            while (pagedRequests.HasPages)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() => _graphApiClient.DeleteFundingLines(pagedRequests
                    .NextPage()
                    .ToArray()));

                EnsureApiCallSucceeded(response, "Unable to delete previous graph relationship for calculation ids and calculation ids");
            }
        }

        private async Task DeleteCalculationRelationships(IEnumerable<CalculationRelationship> calculationRelationships)
        {
            PagedContext<AmendRelationshipRequestModel> pagedRequests = new PagedContext<AmendRelationshipRequestModel>(calculationRelationships
                    .Select(_ => new AmendRelationshipRequestModel
                    {
                        IdA = _.CalculationOneId,
                        IdB = _.CalculationTwoId
                    }),
                PageSize);

            while (pagedRequests.HasPages)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() => _graphApiClient.DeleteCalculationCalculationRelationships(pagedRequests
                    .NextPage()
                    .ToArray()));

                EnsureApiCallSucceeded(response, "Unable to delete previous graph relationship for calculation ids and calculation ids");
            }
        }

        private async Task DeleteDatasetRelationships(IEnumerable<CalculationDataFieldRelationship> datasetRelationships)
        {
            PagedContext<AmendRelationshipRequestModel> pagedRequests = new PagedContext<AmendRelationshipRequestModel>(datasetRelationships
                    .Select(_ => new AmendRelationshipRequestModel
                    {
                        IdA = _.Calculation.CalculationId,
                        IdB = _.DataField.DataFieldId
                    }),
                PageSize);

            while (pagedRequests.HasPages)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() => _graphApiClient.DeleteCalculationDataFieldRelationships(pagedRequests
                    .NextPage()
                    .ToArray()));

                EnsureApiCallSucceeded(response, "Unable to delete previous graph relationships for calculation ids and data field ids");
            }
        }

        private async Task DeleteEnumRelationships(IEnumerable<CalculationEnumRelationship> enumRelationships)
        {
            PagedContext<AmendRelationshipRequestModel> pagedRequests = new PagedContext<AmendRelationshipRequestModel>(enumRelationships
                    .Select(_ => new AmendRelationshipRequestModel
                    {
                        IdA = _.Calculation.CalculationId,
                        IdB = _.Enum.EnumId
                    }),
                PageSize);

            while (pagedRequests.HasPages)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() => _graphApiClient.DeleteCalculationEnumRelationships(pagedRequests
                    .NextPage()
                    .ToArray()));

                EnsureApiCallSucceeded(response, "Unable to delete previous graph relationships for calculation ids and enum ids");
            }
        }

        private async Task UpsertSpecificationNode(Specification specification)
        {
            Guard.ArgumentNotNull(specification, nameof(specification));

            ApiSpecification graphSpecification = _mapper.Map<ApiSpecification>(specification);

            HttpStatusCode response = await _resilience.ExecuteAsync(() =>
                _graphApiClient.UpsertSpecifications(new[]
                {
                    graphSpecification
                }));

            EnsureApiCallSucceeded(response, "Unable to create specification node");
        }

        private async Task UpsertCalculationNodes(IEnumerable<Calculation> calculations)
        {
            Guard.IsNotEmpty(calculations, nameof(calculations));

            PagedContext<ApiCalculation> pagedRequests = new PagedContext<ApiCalculation>(_mapper.Map<IEnumerable<ApiCalculation>>(calculations),
                PageSize);

            while (pagedRequests.HasPages)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertCalculations(pagedRequests
                        .NextPage()
                        .ToArray()));

                EnsureApiCallSucceeded(response, "Unable to create calculation nodes");
            }
        }

        private async Task UpsertSpecificationRelationships(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            string specificationId = specificationCalculationRelationships.Specification.SpecificationId;

            IEnumerable<string> calculationIds = specificationCalculationRelationships.Calculations.Select(_ => _.CalculationId);

            PagedContext<AmendRelationshipRequestModel> pagedRequests = new PagedContext<AmendRelationshipRequestModel>(calculationIds
                    .Select(_ => new AmendRelationshipRequestModel
                    {
                        IdA = _,
                        IdB = specificationId
                    }),
                PageSize);

            while (pagedRequests.HasPages)
            {
                HttpStatusCode response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertCalculationSpecificationRelationships(pagedRequests
                        .NextPage()
                        .ToArray()));

                EnsureApiCallSucceeded(response, "Unable to create specification calculation relationships");
            }
        }

        private async Task UpsertCalculationRelationships(IEnumerable<CalculationRelationship> calculationRelationships)
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

        private async Task UpsertDataFieldRelationships(IEnumerable<CalculationDataFieldRelationship> datasetRelationships)
        {
            IEnumerable<IGrouping<string, CalculationDataFieldRelationship>> relationshipsPerCalculation =
                datasetRelationships.GroupBy(_ => _.Calculation.CalculationId);

            foreach (IGrouping<string, CalculationDataFieldRelationship> relationships in relationshipsPerCalculation)
            {
                HttpStatusCode response;

                ApiDataField[] apiDataFields = relationships.Select(_ => _mapper.Map<ApiDataField>(_.DataField)).ToArray();

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertDataFields(apiDataFields));

                EnsureApiCallSucceeded(response, "Unable to create data field");

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertCalculationDataFieldsRelationships(relationships.Key,
                        relationships.Select(_ => _.DataField.DataFieldId).ToArray()));

                EnsureApiCallSucceeded(response, "Unable to create data field calculation relationships");
            }
        }
        
        private async Task UpsertEnumRelationships(IEnumerable<CalculationEnumRelationship> enumRelationships)
        {
            IEnumerable<IGrouping<string, CalculationEnumRelationship>> relationshipsPerCalculation =
                enumRelationships.GroupBy(_ => _.Calculation.CalculationId);

            foreach (IGrouping<string, CalculationEnumRelationship> relationships in relationshipsPerCalculation)
            {
                HttpStatusCode response;

                ApiEnum[] apiEnums = relationships.Select(_ => _mapper.Map<ApiEnum>(_.Enum)).DistinctBy(_ => _.EnumId).ToArray();

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertEnums(apiEnums));

                EnsureApiCallSucceeded(response, "Unable to create enums");

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertCalculationEnumRelationships(relationships.Select(_ => new AmendRelationshipRequestModel{
                        IdA = _.Calculation.CalculationId,
                        IdB = _.Enum.EnumId
                    }).ToArraySafe()));

                EnsureApiCallSucceeded(response, "Unable to create enum calculation relationships");
            }
        }

        private async Task UpsertDatasetDataFieldRelationships(IEnumerable<DatasetDataFieldRelationship> datasetDataFieldRelationships,
            string specificationId)
        {
            IEnumerable<IGrouping<string, DatasetDataFieldRelationship>> relationshipsPerDataset =
                datasetDataFieldRelationships.GroupBy(_ => _.Dataset.DatasetId);

            foreach (IGrouping<string, DatasetDataFieldRelationship> relationships in relationshipsPerDataset)
            {
                HttpStatusCode response;

                ApiDataset apiDataset = relationships.Select(_ => _mapper.Map<ApiDataset>(_.Dataset)).First();

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertDataset(apiDataset));

                EnsureApiCallSucceeded(response, "Unable to create dataset");

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertSpecificationDatasetRelationship(specificationId, relationships.Key));

                EnsureApiCallSucceeded(response, "Unable to create specification dataset relationship");

                PagedContext<AmendRelationshipRequestModel> requestPages = new PagedContext<AmendRelationshipRequestModel>(relationships
                        .Select(_ => new AmendRelationshipRequestModel
                        {
                            IdA = relationships.Key,
                            IdB = _.DataField.DataFieldId
                        }),
                    PageSize);

                while (requestPages.HasPages)
                {
                    response = await _resilience.ExecuteAsync(() => _graphApiClient.UpsertDatasetDataFieldRelationships(requestPages
                        .NextPage()
                        .ToArray()));

                    EnsureApiCallSucceeded(response, "Unable to create dataset data field relationships");
                }
            }
        }

        private async Task UpsertFundingLines(IEnumerable<FundingLineCalculationRelationship> fundingLineCalculationRelationships)
        {
            HttpStatusCode response;

            ApiFundingLine[] apiFundingLines = fundingLineCalculationRelationships.Select(_ => _mapper.Map<ApiFundingLine>(_.FundingLine)).DistinctBy(_ => _.FundingLineId).ToArray();

            response = await _resilience.ExecuteAsync(() =>
                _graphApiClient.UpsertFundingLines(apiFundingLines));

            EnsureApiCallSucceeded(response, "Unable to create funding lines");

            PagedContext<AmendRelationshipRequestModel> requestPages = new PagedContext<AmendRelationshipRequestModel>(fundingLineCalculationRelationships
                    .Select(_ => new AmendRelationshipRequestModel
                    {
                        IdA = _.CalculationOneId,
                        IdB = _.FundingLine.SpecificationFundingLineId
                    }),
                PageSize);

            while (requestPages.HasPages)
            {
                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertCalculationFundingLineRelationships(requestPages
                        .NextPage()
                        .ToArray()));

                EnsureApiCallSucceeded(response, "Unable to create relationship between calculations and funding lines");
            }

            requestPages = new PagedContext<AmendRelationshipRequestModel>(fundingLineCalculationRelationships
                    .Select(_ => new AmendRelationshipRequestModel
                    {
                        IdA = _.FundingLine.SpecificationFundingLineId,
                        IdB = _.CalculationTwoId
                    }),
                PageSize);

            while (requestPages.HasPages)
            {
                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertFundingLineCalculationRelationships(requestPages
                        .NextPage()
                        .ToArray()));

                EnsureApiCallSucceeded(response, "Unable to create relationship between funding lines and calculations");
            }
        }

        private async Task UpsertDatasetDatasetDefinitionRelationships(IEnumerable<DatasetDatasetDefinitionRelationship> datasetDatasetDefinitionRelationships)
        {
            IEnumerable<IGrouping<string, DatasetDatasetDefinitionRelationship>> relationshipsPerDataset =
                datasetDatasetDefinitionRelationships.GroupBy(_ => _.Dataset.DatasetId);

            foreach (IGrouping<string, DatasetDatasetDefinitionRelationship> relationships in relationshipsPerDataset)
            {
                HttpStatusCode response;

                IEnumerable<ApiDatasetDefinitionValueType> apiDatasetDefinitions = relationships.Select(_ => new ApiDatasetDefinitionValueType
                {
                    DatasetDefinition = _mapper.Map<ApiDatasetDefinition>(_.DatasetDefinition)
                }).ToHashSet();

                response = await _resilience.ExecuteAsync(() =>
                    _graphApiClient.UpsertDatasetDefinitions(apiDatasetDefinitions.Select(_ => _.DatasetDefinition).ToArray()));

                EnsureApiCallSucceeded(response, "Unable to create dataset definitions");

                PagedContext<AmendRelationshipRequestModel> requestPages = new PagedContext<AmendRelationshipRequestModel>(relationships
                        .Select(_ => new AmendRelationshipRequestModel
                        {
                            IdA = _.DatasetDefinition.DatasetDefinitionId,
                            IdB = relationships.Key
                        }),
                    PageSize);

                while (requestPages.HasPages)
                {
                    response = await _resilience.ExecuteAsync(() => _graphApiClient.UpsertDataDefinitionDatasetRelationships(requestPages
                        .NextPage()
                        .ToArray()));

                    EnsureApiCallSucceeded(response, "Unable to create dataset dataset definition relationships");
                }
            }
        }

        private class ApiDatasetDefinitionValueType
        {
            public ApiDatasetDefinition DatasetDefinition { get; set; }

            public override bool Equals(object obj) => GetHashCode().Equals(obj?.GetHashCode());

            public override int GetHashCode() => HashCode.Combine(DatasetDefinition?.DatasetDefinitionId,
                DatasetDefinition?.Name,
                DatasetDefinition?.Description);
        }

        private void EnsureApiCallSucceeded(HttpStatusCode statusCode,
            string errorMessage)
        {
            if (statusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}