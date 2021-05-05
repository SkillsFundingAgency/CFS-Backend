using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
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

            await DetectAndCreateObsoleteItems(fundingLineDetectionParameters);
        }

        private async Task DetectAndCreateObsoleteItems(FundingLineDetectionParameters fundingLineDetectionParameters)
        {
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

            Dictionary<uint, IEnumerable<string>> previousAllowedEnumValuesByTemplateCalculationId = previousTemplateMetadata.Calculations.Where(_ => _.Type == Common.TemplateMetadata.Enums.CalculationType.Enum)
                .ToDictionary(_ => _.TemplateCalculationId, _ => _.AllowedEnumTypeValues);

            IEnumerable<(Enum EnumObject, uint TemplateCalculationId)> templateCalculationEnumValuesObsolete = templateMetadata.Calculations
                .Where(_ => _.Type == Common.TemplateMetadata.Enums.CalculationType.Enum &&
                                        previousAllowedEnumValuesByTemplateCalculationId.ContainsKey(_.TemplateCalculationId) &&
                                        !previousAllowedEnumValuesByTemplateCalculationId[_.TemplateCalculationId].SequenceEqual(_.AllowedEnumTypeValues))
                .SelectMany(_ => previousAllowedEnumValuesByTemplateCalculationId[_.TemplateCalculationId]
                                        .Except(_.AllowedEnumTypeValues)
                                        .Select(t => (new Enum { FundingStreamId = fundingStreamId, 
                                                                SpecificationId = specificationId, 
                                                                EnumName = _typeIdentifierGenerator.GenerateIdentifier($"{_.Name}Options"), 
                                                                EnumValue = _typeIdentifierGenerator.GenerateIdentifier(t),
                                                                EnumValueName = t},_.TemplateCalculationId)))
                .DistinctBy<(Enum EnumObject, uint TemplateCalculationId), string>(_ => _.EnumObject.EnumId);

            foreach((Enum EnumObject, uint TemplateCalculationId) enumTuple in templateCalculationEnumValuesObsolete)
            {
                ApiResponse<IEnumerable<Entity<Enum>>> enumEntitiesApiResponse =
                await _graph.GetAllEntitiesRelatedToEnum(enumTuple.EnumObject.EnumId);

                if (enumEntitiesApiResponse?.Content != null)
                {
                    IEnumerable<Entity<Enum>> enumEntities = enumEntitiesApiResponse.Content;

                    IEnumerable<Calculation> enumCalculations = enumEntities
                        .Where(_ => _.Relationships != null)
                        .SelectMany(_ => _.Relationships.Where(rel => rel.Type.Equals(CalculationEnumRelationship.ToIdField, StringComparison.InvariantCultureIgnoreCase)))
                        .Select(rel => ((object)rel.One).AsJson().AsPoco<Calculation>())
                        .Distinct();

                    ObsoleteItem obsoleteItem = new ObsoleteItem
                    {
                        SpecificationId = specificationId,
                        FundingStreamId = fundingStreamId,
                        CodeReference = enumTuple.EnumObject.CodeReference,
                        EnumValueName = enumTuple.EnumObject.EnumValueName,
                        TemplateCalculationId = enumTuple.TemplateCalculationId,
                        CalculationIds = enumCalculations.Select(_ => _.CalculationId).ToList(),
                        ItemType = ObsoleteItemType.EnumValue,
                        Id = _uniqueIdentifierProvider.CreateUniqueIdentifier()
                    };

                    ApiResponse<ObsoleteItem> obsoleteItemResponse = await _calculationsPolicy.ExecuteAsync(() =>
                            _calculations.CreateObsoleteItem(obsoleteItem));

                    if (!obsoleteItemResponse.StatusCode.IsSuccess())
                    {
                        string message = $"Unable to create obsolete item for enum - {enumTuple.EnumObject.EnumId}.";

                        LogInformation(message);

                        throw new Exception(message);
                    }
                }
            }
                
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
                return;
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
                .Select(rel => ((object) rel.One).AsJson().AsPoco<Calculation>())
                .Distinct();

            foreach (TemplateMetadataFundingLine fundingLine in missingFundingLines)
            {
                ApiResponse<IEnumerable<Entity<FundingLine>>> fundingLineEntitiesApiResponse =
                    await _graph.GetAllEntitiesRelatedToFundingLine($"{specificationId}-{fundingLine.FundingLineCode}");

                if (fundingLineEntitiesApiResponse?.Content == null)
                {
                    string message = $"Unable to find the graph entities for funding line - {fundingLine}.";

                    LogError(message);

                    throw new Exception(message);
                }

                IEnumerable<Entity<FundingLine>> fundingLineGraphEntries = fundingLineEntitiesApiResponse.Content;

                IEnumerable<string> fundingLineCalculationIds = fundingLineGraphEntries.Where(_ => _.Relationships != null)
                    .SelectMany(_ => _.Relationships.Where(rel => rel.Type.Equals(FundingLineCalculationRelationship.FromIdField, StringComparison.InvariantCultureIgnoreCase)))
                    .Select(rel => ((object) rel.Two).AsJson().AsPoco<Calculation>().CalculationId)
                    .Distinct();
                
                //TODO; refactor out all of these dictionaries everywhere into a more of a model

                // Calculations in specification and in missing funding line
                IDictionary<string, IEnumerable<string>> fundingLineCalculationIdsByTemplateCalculationId
                    = specificationCalculations
                        .Where(_ => fundingLineCalculationIds.Contains(_.CalculationId))
                        .GroupBy(_ => _.TemplateCalculationId)
                        .ToDictionary(_ => _.Key,
                            _ => _.Select(calc => calc.CalculationId)
                                .Distinct());

                if (fundingLineCalculationIdsByTemplateCalculationId.Any())
                {
                    foreach (KeyValuePair<string, IEnumerable<string>> calculationsByTemplateId in fundingLineCalculationIdsByTemplateCalculationId
                        .Where(_ => _.Value.Any()))
                    {
                        ObsoleteItem obsoleteItem = new ObsoleteItem
                        {
                            SpecificationId = specificationId,
                            FundingLineId = fundingLine.TemplateLineId,
                            FundingStreamId = fundingStreamId,
                            CodeReference = _typeIdentifierGenerator.GenerateIdentifier(fundingLine.Name),
                            TemplateCalculationId = Convert.ToUInt32(calculationsByTemplateId.Key),
                            CalculationIds = calculationsByTemplateId.Value.ToList(),
                            ItemType = ObsoleteItemType.FundingLine,
                            Id = _uniqueIdentifierProvider.CreateUniqueIdentifier(),
                            FundingLineName = fundingLine.Name
                        };

                        ApiResponse<ObsoleteItem> obsoleteItemResponse = await _calculationsPolicy.ExecuteAsync(() =>
                            _calculations.CreateObsoleteItem(obsoleteItem));

                        if (!obsoleteItemResponse.StatusCode.IsSuccess())
                        {
                            string message = $"Unable to create obsolete item for funding line template id - {fundingLine}.";
                            
                            LogInformation(message);
                            
                            throw new Exception(message);
                        }
                    }
                }
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

            foreach (ObsoleteItem obsoleteItem in obsoleteItemsResponse.Content)
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