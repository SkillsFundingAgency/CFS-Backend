using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;
using FundingLineType = CalculateFunding.Models.Publishing.FundingLineType;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using System;
using System.Threading;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderDataGenerator : IPublishedProviderDataGenerator
    {
        private readonly IFundingLineTotalAggregator _fundingLineTotalAggregator;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public PublishedProviderDataGenerator(ILogger logger, IFundingLineTotalAggregator fundingLineTotalAggregator, IMapper mapper)
        {
            Guard.ArgumentNotNull(fundingLineTotalAggregator, nameof(fundingLineTotalAggregator));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _fundingLineTotalAggregator = fundingLineTotalAggregator;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Generate Funding Lines, Calculations and Reference Data for a funding stream for all in scope providers
        /// </summary>
        /// <param name="templateMetadata">Template Metadata</param>
        /// <param name="templateMapping">Template Mapping</param>
        /// <param name="scopedProviders">Scoped providers for a specification</param>
        /// <param name="calculationResults">Calculation Results</param>
        /// <returns>Dictionary of Generated Provider Results, keyed on ProviderId</returns>
        public IDictionary<string, GeneratedProviderResult> Generate(TemplateMetadataContents templateMetadata, Common.ApiClient.Calcs.Models.TemplateMapping templateMapping, IEnumerable<Provider> scopedProviders, IDictionary<string, ProviderCalculationResult> calculationResults)
        {
            Guard.ArgumentNotNull(templateMetadata, nameof(templateMetadata));
            Guard.ArgumentNotNull(templateMapping, nameof(templateMapping));
            Guard.ArgumentNotNull(calculationResults, nameof(calculationResults));

            ConcurrentDictionary<string, GeneratedProviderResult> results = new ConcurrentDictionary<string, GeneratedProviderResult>();

            TimeSpan loggingPeriod = TimeSpan.FromMinutes(5);

            using (new Timer(
                _ => _logger.Information($"{results.Count}: Published Providers processed."),
                null, loggingPeriod, loggingPeriod))

            Parallel.ForEach(scopedProviders, new ParallelOptions { MaxDegreeOfParallelism = 15 }, (provider) =>
            {
                GeneratedProviderResult generatedProviderResult = new GeneratedProviderResult();

                ProviderCalculationResult calculationResultsForProvider = null;
                calculationResults.TryGetValue(provider.ProviderId, out calculationResultsForProvider);

                if (calculationResultsForProvider != null)
                {
                    GeneratorModels.FundingValue fundingValue = _fundingLineTotalAggregator.GenerateTotals(templateMetadata, templateMapping.TemplateMappingItems.ToDictionary(_ => _.TemplateId), calculationResultsForProvider.Results.ToDictionary(_ => _.Id));

                    // Get funding lines
                    IEnumerable<GeneratorModels.FundingLine> fundingLines = fundingValue.FundingLines?.Flatten(_ => _.FundingLines) ?? new GeneratorModels.FundingLine[0];

                    Dictionary<uint, GeneratorModels.FundingLine> uniqueFundingLine = new Dictionary<uint, GeneratorModels.FundingLine>();

                    generatedProviderResult.FundingLines = fundingLines.Where(_ => uniqueFundingLine.TryAdd(_.TemplateLineId, _)).Select(_ =>
                    {
                        return _mapper.Map<FundingLine>(_);
                    }).ToList();

                    // Set total funding
                    IEnumerable<FundingLine> allFundingLinesWithValues = generatedProviderResult.FundingLines
                            .Where(_ => _.Type == FundingLineType.Payment && _.Value.HasValue);

                    generatedProviderResult.TotalFunding = allFundingLinesWithValues.AnyWithNullCheck() ? allFundingLinesWithValues
                            .Sum(p =>
                            {
                                return p.Value;
                            }) : null;

                    Dictionary<uint, GeneratorModels.Calculation> uniqueCalculations = new Dictionary<uint, GeneratorModels.Calculation>();

                    // Get calculations
                    IEnumerable<GeneratorModels.Calculation> fundingCalculations = uniqueFundingLine.Values?.SelectMany(_ => _.Calculations.Flatten(calc => calc.Calculations)) ?? new GeneratorModels.Calculation[0];

                    generatedProviderResult.Calculations = fundingCalculations.Where(_ => uniqueCalculations.TryAdd(_.TemplateCalculationId, _)).Select(_ =>
                    {
                        return _mapper.Map<FundingCalculation>(_);
                    }).ToList();

                    // Set Provider information
                    generatedProviderResult.Provider = _mapper.Map<Provider>(provider);

                    results.TryAdd(provider.ProviderId, generatedProviderResult);
                }
            });

            return results;
        }
    }
}
