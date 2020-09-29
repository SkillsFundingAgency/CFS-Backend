using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderFundingStructureService : IPublishedProviderFundingStructureService
    {
        private readonly ILogger _logger;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private ISpecificationService _specificationService;
        private readonly IPoliciesService _policiesService;
        private readonly ICalculationsService _calculationsService;

        public PublishedProviderFundingStructureService(
            ILogger logger,
            IPublishedFundingRepository publishedFundingRepository,
            ISpecificationService specificationService,
            IPoliciesService policiesService,
            ICalculationsService calculationsService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(calculationsService, nameof(calculationsService));

            _logger = logger;
            _publishedFundingRepository = publishedFundingRepository;
            _specificationService = specificationService;
            _policiesService = policiesService;
            _calculationsService = calculationsService;
        }
        public async Task<IActionResult> GetPublishedProviderFundingStructure(string publishedProviderVersionId)
        {
            Guard.IsNullOrWhiteSpace(publishedProviderVersionId, nameof(publishedProviderVersionId));

            PublishedProviderVersion publishedProviderVersion = await _publishedFundingRepository.GetPublishedProviderVersionById(publishedProviderVersionId);

            if(publishedProviderVersion == null)
            {
                return new NotFoundObjectResult($"Published provider version not found for the given PublishedProviderVersionId - {publishedProviderVersionId}");
            }

            string specificationId = publishedProviderVersion.SpecificationId;
            string fundingStreamId = publishedProviderVersion.FundingStreamId;
            string fundingPeriodId = publishedProviderVersion.FundingPeriodId;

            SpecificationSummary specificationSummary = await _specificationService.GetSpecificationSummaryById(specificationId);
            if (specificationSummary == null)
            {
                return new NotFoundObjectResult($"Specification not found for SpecificationId - {specificationId}");
            }

            string templateVersion = specificationSummary.TemplateIds.ContainsKey(fundingStreamId)
                ? specificationSummary.TemplateIds[fundingStreamId]
                : null;

            if (templateVersion == null)
            {
                return new InternalServerErrorResult($"Specification contains no matching template version for funding stream '{fundingStreamId}'");
            }

            TemplateMetadataContents fundingTemplateContents = await _policiesService.GetTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion);

            if (fundingTemplateContents == null)
            {
                return new InternalServerErrorResult($"Unable to locate funding template contents for {fundingStreamId} {fundingPeriodId} {templateVersion}");
            }

            TemplateMapping templateMapping = await _calculationsService.GetTemplateMapping(specificationId, fundingStreamId);

            List<PublishedProviderFundingStructureItem> fundingStructures = new List<PublishedProviderFundingStructureItem>();
            RecursivelyAddFundingLineToFundingStructure(
                fundingStructures,
                fundingTemplateContents.RootFundingLines,
                templateMapping.TemplateMappingItems.ToList(),
                publishedProviderVersion);

            PublishedProviderFundingStructure fundingStructure = new PublishedProviderFundingStructure
            {
                Items = fundingStructures,
                PublishedProviderVersion = publishedProviderVersion.Version
            };

            return new OkObjectResult(fundingStructure);
        }

        private static void RecursivelyAddFundingLineToFundingStructure(
            List<PublishedProviderFundingStructureItem> fundingStructures,
            IEnumerable<Common.TemplateMetadata.Models.FundingLine> fundingLines,
            List<TemplateMappingItem> templateMappingItems,
            PublishedProviderVersion publishedProviderVersion,
            int level = 0) =>
            fundingStructures.AddRange(fundingLines.Select(fundingLine =>
                RecursivelyAddFundingLines(
                    fundingLine.FundingLines,
                    templateMappingItems,
                    level,
                    fundingLine,
                    publishedProviderVersion)));

        private static PublishedProviderFundingStructureItem RecursivelyAddFundingLines(
            IEnumerable<Common.TemplateMetadata.Models.FundingLine> fundingLines,
            List<TemplateMappingItem> templateMappingItems,
            int level,
            Common.TemplateMetadata.Models.FundingLine fundingLine,
            PublishedProviderVersion publishedProviderVersion)
        {
            level++;

            List<PublishedProviderFundingStructureItem> innerFundingStructureItems = new List<PublishedProviderFundingStructureItem>();

            // If funding line has calculations, recursively add them to list of inner FundingStructureItems
            if (fundingLine.Calculations != null && fundingLine.Calculations.Any())
            {
                foreach (Common.TemplateMetadata.Models.Calculation calculation in fundingLine.Calculations)
                {
                    innerFundingStructureItems.Add(
                        RecursivelyMapCalculationsToFundingStructureItem(
                            calculation,
                            level,
                            templateMappingItems,
                            publishedProviderVersion));
                }
            }

            // If funding line has more funding lines, recursively add them to list of inner FundingStructureItems
            if (fundingLine.FundingLines != null && fundingLine.FundingLines.Any())
            {
                foreach (Common.TemplateMetadata.Models.FundingLine line in fundingLines)
                {
                    innerFundingStructureItems.Add(RecursivelyAddFundingLines(
                        line.FundingLines,
                        templateMappingItems,
                        level,
                        line,
                        publishedProviderVersion));
                }
            }

            CalculateFunding.Models.Publishing.FundingLine publishedProviderFundingLine = publishedProviderVersion.FundingLines.FirstOrDefault(_ => _.TemplateLineId == fundingLine.TemplateLineId);
            string calculationValue = null;
            if (publishedProviderFundingLine != null)
            {
                calculationValue = publishedProviderFundingLine.Value.AsFormatCalculationType(CalculationValueFormat.Number);
            }

            // Add FundingStructureItem
            PublishedProviderFundingStructureItem fundingStructureItem = MapToFundingStructureItem(
                level,
                fundingLine.Name,
                PublishedProviderFundingStructureType.FundingLine,
                null,
                null,
                innerFundingStructureItems.Any() ? innerFundingStructureItems : null,
                calculationValue);

            return fundingStructureItem;
        }

        private static PublishedProviderFundingStructureItem RecursivelyMapCalculationsToFundingStructureItem(Common.TemplateMetadata.Models.Calculation calculation,
            int level,
            List<TemplateMappingItem> templateMappingItems,
            PublishedProviderVersion publishedProviderVersion)
        {
            level++;

            List<PublishedProviderFundingStructureItem> innerFundingStructureItems = null;

            string calculationId = GetCalculationId(calculation, templateMappingItems);
            FundingCalculation fundingCalculation = publishedProviderVersion.Calculations.FirstOrDefault(_ => _.TemplateCalculationId == calculation.TemplateCalculationId);

            string calculationType = null;
            string calculationValue = null;

            if (fundingCalculation != null)
            {
                calculationType = calculation.Type.ToString();
                calculationValue = fundingCalculation.Value.AsFormatCalculationType(calculation.ValueFormat);
            }

            if (calculation.Calculations != null && calculation.Calculations.Any())
            {
                innerFundingStructureItems = calculation.Calculations.Select(innerCalculation =>
                        RecursivelyMapCalculationsToFundingStructureItem(
                            innerCalculation,
                            level,
                            templateMappingItems,
                            publishedProviderVersion))
                    .ToList();
            }

            return MapToFundingStructureItem(
                level,
                calculation.Name,
                PublishedProviderFundingStructureType.Calculation,
                calculationType,
                calculationId,
                innerFundingStructureItems,
                calculationValue);
        }

        private static PublishedProviderFundingStructureItem MapToFundingStructureItem(
            int level,
            string name,
            PublishedProviderFundingStructureType type,
            string calculationType = null,
            string calculationId = null,
            List<PublishedProviderFundingStructureItem> fundingStructureItems = null,
            string value = null) =>
            new PublishedProviderFundingStructureItem(
                level,
                name,
                calculationId,
                type,
                value,
                calculationType,
                fundingStructureItems);

        private static string GetCalculationId(
            Common.TemplateMetadata.Models.Calculation calculation,
            IEnumerable<TemplateMappingItem> templateMappingItems) =>
            templateMappingItems
                .FirstOrDefault(_ => _.TemplateId == calculation.TemplateCalculationId)?
                .CalculationId;
    }
}
