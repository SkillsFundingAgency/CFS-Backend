using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Calcs.Models.ObsoleteItems;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using Calculation = CalculateFunding.Models.Graph.Calculation;
using Enum = CalculateFunding.Common.ApiClient.Graph.Models.Enum;
using FundingLine = CalculateFunding.Common.ApiClient.Graph.Models.FundingLine;
using Specification = CalculateFunding.Common.ApiClient.Graph.Models.Specification;

namespace CalculateFunding.Services.Specs.ObsoleteItems
{
    public class ObsoleteFundingLineAndEnumDetection : JobProcessingService, IObsoleteFundingLineAndEnumDetection
    {
        private readonly IUniqueIdentifierProvider _uniqueIdentifierProvider;
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;
        private readonly ICalculationsApiClient _calculations;
        private readonly IPoliciesApiClient _policies;
        private readonly IGraphApiClient _graph;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _calculationsPolicy;
        private readonly AsyncPolicy _policiesPolicy;

        public ObsoleteFundingLineAndEnumDetection(ICalculationsApiClient calculations,
            IPoliciesApiClient policies,
            IGraphApiClient graph,
            IUniqueIdentifierProvider uniqueIdentifierProvider,
            ISpecificationsResiliencePolicies resiliencePolicies,
            ITypeIdentifierGenerator typeIdentifierGenerator,
            IJobManagement jobManagement,
            ILogger logger) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(calculations, nameof(calculations));
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(graph, nameof(graph));
            Guard.ArgumentNotNull(uniqueIdentifierProvider, nameof(uniqueIdentifierProvider));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.CalcsApiClient, nameof(resiliencePolicies.CalcsApiClient));
            Guard.ArgumentNotNull(typeIdentifierGenerator, nameof(typeIdentifierGenerator));
            
            _calculations = calculations;
            _policies = policies;
            _graph = graph;
            _uniqueIdentifierProvider = uniqueIdentifierProvider;
            _logger = logger;
            _typeIdentifierGenerator = typeIdentifierGenerator;
            _calculationsPolicy = resiliencePolicies.CalcsApiClient;
            _policiesPolicy = resiliencePolicies.PoliciesApiClient;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            FundingLineDetectionParameters fundingLineDetectionParameters = message;

            string specificationId = fundingLineDetectionParameters.SpecificationId;
            string fundingStreamId = fundingLineDetectionParameters.FundingStreamId;
            string fundingPeriodId = fundingLineDetectionParameters.FundingPeriodId;
            string previousTemplateVersionId = fundingLineDetectionParameters.PreviousTemplateVersionId;
            string templateVersionId = fundingLineDetectionParameters.TemplateVersionId;

            await ClearDownObsoleteItems(specificationId);

            Task<TemplateMetadataDistinctContents> previousTemplateMetadataTask = GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, previousTemplateVersionId);
            Task<TemplateMetadataDistinctContents> templateMetadataTask = GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersionId);

            await TaskHelper.WhenAllAndThrow(previousTemplateMetadataTask, templateMetadataTask);

            TemplateMetadataDistinctContents previousTemplateMetadata = previousTemplateMetadataTask.Result;
            TemplateMetadataDistinctContents templateMetadata = templateMetadataTask.Result;

            foreach (ObsoleteItem enumObsoleteItem in await DetectEnumObsoleteItems(previousTemplateMetadata, templateMetadata, specificationId, fundingStreamId))
            {
                await CreateObsoleteItem(enumObsoleteItem, $"Unable to create obsolete item for enum - {enumObsoleteItem.EnumValueName}.");
            }

            foreach (ObsoleteItem fundingLineObsoleteItem in await DetectFundingLineObsoleteItems(previousTemplateMetadata, templateMetadata, specificationId, fundingStreamId))
            {
                await CreateObsoleteItem(fundingLineObsoleteItem, $"Unable to create obsolete item for funding line template id - {fundingLineObsoleteItem.FundingLineId}.");
            }
        }

        private async Task<IEnumerable<ObsoleteItem>> DetectEnumObsoleteItems(TemplateMetadataDistinctContents previousTemplateMetadata, TemplateMetadataDistinctContents templateMetadata, string specificationId, string fundingStreamId)
        {
            List<ObsoleteItem> obsoleteItems = new List<ObsoleteItem>();
            
            foreach (TemplateCalculationEnumDetail templateCalculationEnum in GetTemplateCalculationEnumNamesObsolete(previousTemplateMetadata, templateMetadata, specificationId, fundingStreamId))
            {
                ApiResponse<IEnumerable<Entity<Enum>>> enumEntitiesApiResponse =
                    await _graph.GetAllEntitiesRelatedToEnum(templateCalculationEnum.Enum.EnumId);

                if (enumEntitiesApiResponse?.Content != null)
                {
                    IEnumerable<Entity<Enum>> enumEntities = enumEntitiesApiResponse.Content;

                    if (enumEntities.IsNullOrEmpty())
                    {
                        continue;
                    }

                    IEnumerable<Calculation> enumCalculations = enumEntities
                        .Where(_ => _.Relationships != null)
                        .SelectMany(_ => _.Relationships.Where(rel => rel.Type.Equals(CalculationEnumRelationship.ToIdField, StringComparison.InvariantCultureIgnoreCase)))
                        .Select(rel => ((object)rel.One).AsJson().AsPoco<Calculation>())
                        .Distinct();

                    obsoleteItems.Add(new ObsoleteItem
                    {
                        SpecificationId = specificationId,
                        FundingStreamId = fundingStreamId,
                        CodeReference = templateCalculationEnum.Enum.CodeReference,
                        EnumValueName = templateCalculationEnum.Enum.EnumValueName,
                        TemplateCalculationId = templateCalculationEnum.TemplateCalculation.TemplateCalculationId,
                        CalculationIds = enumCalculations.Select(_ => _.CalculationId).ToList(),
                        ItemType = ObsoleteItemType.EnumValue,
                        Id = _uniqueIdentifierProvider.CreateUniqueIdentifier()
                    });
                }
            }

            return obsoleteItems;
        }

        private IEnumerable<TemplateCalculationEnumDetail> GetTemplateCalculationEnumNamesObsolete(TemplateMetadataDistinctContents previousTemplateMetadata, TemplateMetadataDistinctContents templateMetadata, string specificationId, string fundingStreamId)
        {
            Dictionary<uint, IEnumerable<string>> previousAllowedEnumValuesByTemplateCalculationId = previousTemplateMetadata.Calculations.Where(_ => _.Type == Common.TemplateMetadata.Enums.CalculationType.Enum)
                .ToDictionary(_ => _.TemplateCalculationId, _ => _.AllowedEnumTypeValues);

            // get all enum template calculations where the allowed enum values don't match with the previous template enum calculation
            IEnumerable<TemplateMetadataCalculation> unMatchedEnumCalculations = templateMetadata.Calculations
                .Where(_ => _.Type == Common.TemplateMetadata.Enums.CalculationType.Enum &&
                                        previousAllowedEnumValuesByTemplateCalculationId.ContainsKey(_.TemplateCalculationId) &&
                                        !previousAllowedEnumValuesByTemplateCalculationId[_.TemplateCalculationId].SequenceEqual(_.AllowedEnumTypeValues));

            // get all enum names which have been removed against the template calculation
            IEnumerable<TemplateCalculationEnum> templateCalculationWithMissingEnums = unMatchedEnumCalculations.SelectMany(_ => previousAllowedEnumValuesByTemplateCalculationId[_.TemplateCalculationId]
                                        .Except(_.AllowedEnumTypeValues)
                                        .Select(t => new TemplateCalculationEnum { 
                                            TemplateCalculation = _,
                                            EnumName = t
                                        }))
                                        .DistinctBy(_ => _.EnumName);

            foreach (TemplateCalculationEnum templateCalculationMissingEnum in templateCalculationWithMissingEnums)
            {
                yield return new TemplateCalculationEnumDetail
                {
                    Enum = new Enum
                    {
                        FundingStreamId = fundingStreamId,
                        SpecificationId = specificationId,
                        EnumName = _typeIdentifierGenerator.GenerateIdentifier($"{templateCalculationMissingEnum.TemplateCalculation.Name}Options"),
                        EnumValue = _typeIdentifierGenerator.GenerateIdentifier(templateCalculationMissingEnum.EnumName),
                        EnumValueName = templateCalculationMissingEnum.EnumName
                    },
                    TemplateCalculation = templateCalculationMissingEnum.TemplateCalculation
                };
            }
        }

        private async Task<IEnumerable<ObsoleteItem>> DetectFundingLineObsoleteItems(TemplateMetadataDistinctContents previousTemplateMetadata, TemplateMetadataDistinctContents templateMetadata, string specificationId, string fundingStreamId)
        {
            List<ObsoleteItem> obsoleteItems = new List<ObsoleteItem>();

            HashSet<uint> previousFundingLineTemplateIdsExceptLatest = previousTemplateMetadata.FundingLines
                .Select(_ => _.TemplateLineId)
                .ToHashSet();

            previousFundingLineTemplateIdsExceptLatest.ExceptWith(templateMetadata.FundingLines
                .Select(_ => _.TemplateLineId)
                .ToHashSet());

            TemplateMetadataFundingLine[] missingFundingLines = previousTemplateMetadata.FundingLines.Where(_ =>
                    previousFundingLineTemplateIdsExceptLatest.Contains(_.TemplateLineId))
                .ToArray();

            if (!missingFundingLines.Any())
            {
                return obsoleteItems;
            }

            ApiResponse<IEnumerable<Entity<Specification>>> specificationEntitiesApiResponse =
                await _graph.GetAllEntitiesRelatedToSpecification(specificationId);

            if (specificationEntitiesApiResponse?.Content == null)
            {
                string message = $"Unable to find the graph entities for specification - {specificationId}.";

                LogError(message);

                throw new Exception(message);
            }

            IEnumerable<Entity<Specification>> specificationEntities = specificationEntitiesApiResponse.Content;

            IEnumerable<Calculation> specificationCalculations = specificationEntities
                .Where(_ => _.Relationships != null)
                .SelectMany(_ => _.Relationships.Where(rel => rel.Type.Equals(SpecificationCalculationRelationships.FromIdField, StringComparison.InvariantCultureIgnoreCase)))
                .Select(rel => ((object)rel.One).AsJson().AsPoco<Calculation>())
                .Distinct();

            foreach (TemplateMetadataFundingLine fundingLine in missingFundingLines)
            {
                ApiResponse<IEnumerable<Entity<FundingLine>>> fundingLineEntitiesApiResponse =
                    await _graph.GetAllEntitiesRelatedToFundingLine($"{specificationId}-{fundingStreamId}_{fundingLine.TemplateLineId}");

                if (fundingLineEntitiesApiResponse?.Content == null)
                {
                    string message = $"Unable to find the graph entities for funding line - {fundingLine}.";

                    LogError(message);

                    throw new Exception(message);
                }

                IEnumerable<Entity<FundingLine>> fundingLineGraphEntries = fundingLineEntitiesApiResponse.Content;

                IEnumerable<string> fundingLineCalculationIds = fundingLineGraphEntries.Where(_ => _.Relationships != null)
                    .SelectMany(_ => _.Relationships.Where(rel => rel.Type.Equals(FundingLineCalculationRelationship.FromIdField, StringComparison.InvariantCultureIgnoreCase)))
                    .Select(rel => ((object)rel.One).AsJson().AsPoco<Calculation>().CalculationId)
                    .Distinct();

                if (fundingLineCalculationIds.IsNullOrEmpty())
                {
                    continue;
                }

                IDictionary<string, TemplateMappingItem> templateMappingItems = await GetTemplateMappings(specificationId, fundingStreamId);
                
                // Additional calculations in specification that reference missing funding line
                IEnumerable<string> fundingLineAdditionalCalculationIds = specificationCalculations
                        .Where(_ => fundingLineCalculationIds.Contains(_.CalculationId) && !templateMappingItems.ContainsKey(_.CalculationId))
                        .Select(_ => _.CalculationId);

                // create single additional calculations obsolete item
                if (fundingLineAdditionalCalculationIds.Any())
                {
                    obsoleteItems.Add(new ObsoleteItem
                    {
                        SpecificationId = specificationId,
                        FundingLineId = fundingLine.TemplateLineId,
                        FundingStreamId = fundingStreamId,
                        CodeReference = _typeIdentifierGenerator.GenerateIdentifier(fundingLine.Name),
                        CalculationIds = fundingLineAdditionalCalculationIds,
                        ItemType = ObsoleteItemType.FundingLine,
                        Id = _uniqueIdentifierProvider.CreateUniqueIdentifier(),
                        FundingLineName = fundingLine.Name
                    });
                }

                // Template calculations in specification that reference missing funding line
                foreach (FundingLineCalculation calculationsByTemplateId in
                    GetMissingFundingLineTemplateCalculations(templateMappingItems, 
                                                                            specificationCalculations, 
                                                                            fundingLineCalculationIds))
                {
                    obsoleteItems.Add(new ObsoleteItem
                    {
                        SpecificationId = specificationId,
                        FundingLineId = fundingLine.TemplateLineId,
                        FundingStreamId = fundingStreamId,
                        CodeReference = _typeIdentifierGenerator.GenerateIdentifier(fundingLine.Name),
                        CalculationIds = calculationsByTemplateId.CalculationIds,
                        TemplateCalculationId = Convert.ToUInt32(calculationsByTemplateId.FundingLineId),
                        ItemType = ObsoleteItemType.FundingLine,
                        Id = _uniqueIdentifierProvider.CreateUniqueIdentifier(),
                        FundingLineName = fundingLine.Name
                    });
                }
            }

            return obsoleteItems;
        }

        private IEnumerable<FundingLineCalculation> GetMissingFundingLineTemplateCalculations(IDictionary<string, TemplateMappingItem> templateMappingItems,
            IEnumerable<Calculation> specificationCalculations,
            IEnumerable<string> fundingLineCalculationIds)
        {
            foreach(IGrouping<uint, Calculation> grouping in specificationCalculations
                        .Where(_ => fundingLineCalculationIds.Contains(_.CalculationId) && templateMappingItems.ContainsKey(_.CalculationId))
                        .GroupBy(_ => templateMappingItems[_.CalculationId].TemplateId))
            {
                yield return new FundingLineCalculation
                {
                    FundingLineId = grouping.Key,
                    CalculationIds = grouping.Select(calc => calc.CalculationId)
                };
            }   
        }

        private async Task<IDictionary<string, TemplateMappingItem>> GetTemplateMappings(string specificationId, string fundingStreamId)
        {
            ApiResponse<TemplateMapping> apiTemplateMappingResponse = await _calculationsPolicy.ExecuteAsync(() => _calculations.GetTemplateMapping(specificationId, fundingStreamId));

            if (apiTemplateMappingResponse?.Content == null)
            {
                string message = $"Unable to retrieve template mapping for specification - {specificationId} and funding stream - {fundingStreamId}.";

                LogError(message);

                throw new Exception(message);
            }

            return apiTemplateMappingResponse.Content.TemplateMappingItems.Where(_ => _.CalculationId != null).ToDictionary(_ => _.CalculationId);
        }

        private async Task CreateObsoleteItem(ObsoleteItem obsoleteItem, string failureMessage)
        {
            ApiResponse<ObsoleteItem> obsoleteItemResponse = await _calculationsPolicy.ExecuteAsync(() =>
                    _calculations.CreateObsoleteItem(obsoleteItem));

            if (!obsoleteItemResponse.StatusCode.IsSuccess())
            {
                LogInformation(failureMessage);

                throw new Exception(failureMessage);
            }
        }

        private async Task ClearDownObsoleteItems(string specificationId)
        {
            ApiResponse<IEnumerable<ObsoleteItem>> obsoleteItemsResponse = await _calculationsPolicy.ExecuteAsync(() => _calculations.GetObsoleteItemsForSpecification(specificationId));

            if (obsoleteItemsResponse?.Content == null)
            {
                return;
            }

            if (!obsoleteItemsResponse.StatusCode.IsSuccess())
            {
                string message = $"Unable to retrieve obsolete items for specification - {specificationId}.";

                LogInformation(message);

                throw new Exception(message);
            }

            foreach (ObsoleteItem obsoleteItem in obsoleteItemsResponse.Content.Where(_ => !_.IsReleasedData))
            {
                Task<HttpStatusCode>[] tasks = obsoleteItem.CalculationIds.Select(_ => _calculationsPolicy.ExecuteAsync(() => _calculations.RemoveObsoleteItem(obsoleteItem.Id, _))).ToArraySafe();

                await TaskHelper.WhenAllAndThrow(tasks);

                tasks.ForEach(_ =>
                {
                    if (!_.Result.IsSuccess())
                    {
                        string message = $"Unable to delete obsolete item - {obsoleteItem.Id}.";

                        LogInformation(message);

                        throw new Exception(message);
                    }
                });
            }
        }

        private async Task<TemplateMetadataDistinctContents> GetDistinctTemplateMetadataContents(string fundingStreamId,
            string fundingPeriodId,
            string templateVersion)
        {
            ApiResponse<TemplateMetadataDistinctContents> templateMetaDataResponse = await _policiesPolicy.ExecuteAsync(() =>
                _policies.GetDistinctTemplateMetadataContents(fundingStreamId,
                    fundingPeriodId,
                    templateVersion));

            if (templateMetaDataResponse?.Content == null)
            {
                string message =
                    $"Unable to find the template metadata for funding stream - {fundingStreamId}, funding period id - {fundingPeriodId}, template version - {templateVersion}.";

                LogError(message);

                throw new Exception(message);
            }

            return templateMetaDataResponse.Content;
        }

        private void LogInformation(string message) => _logger.Information(FormatMessage(message));

        private void LogError(string message) => _logger.Error(FormatMessage(message));

        private static string FormatMessage(string message) => $"SpecificationTemplateVersionChangedHandler: {message}";
    }
}