using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;
using OrganisationGroupingReason = CalculateFunding.Models.Publishing.OrganisationGroupingReason;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using CalculateFunding.Common.ApiClient.Calcs.Models;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderDataGenerator : IPublishedProviderDataGenerator
    {
        private readonly IFundingLineTotalAggregator _fundingLineTotalAggregator;
        private readonly IMapper _mapper;

        public PublishedProviderDataGenerator(IFundingLineTotalAggregator fundingLineTotalAggregator, IMapper mapper)
        {
            Guard.ArgumentNotNull(fundingLineTotalAggregator, nameof(fundingLineTotalAggregator));

            _fundingLineTotalAggregator = fundingLineTotalAggregator;
            _mapper = mapper;
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

            Parallel.ForEach (scopedProviders,(provider) => 
            {
                GeneratedProviderResult generatedProviderResult = new GeneratedProviderResult();

                ProviderCalculationResult calculationResultsForProvider = null;
                calculationResults.TryGetValue(provider.ProviderId, out calculationResultsForProvider);

                if (calculationResultsForProvider != null)
                {
                    GeneratorModels.FundingValue fundingValue = _fundingLineTotalAggregator.GenerateTotals(templateMetadata, templateMapping, calculationResultsForProvider.Results);

                    // Get funding lines
                    IEnumerable<GeneratorModels.FundingLine> fundingLines = fundingValue.FundingLines?.Flatten(_ => _.FundingLines) ?? new GeneratorModels.FundingLine[0];

                    Dictionary<uint, GeneratorModels.FundingLine> uniqueFundingLine = new Dictionary<uint, GeneratorModels.FundingLine>();
                    
                    generatedProviderResult.FundingLines = fundingLines.Where(_ => uniqueFundingLine.TryAdd(_.TemplateLineId, _)).Select(_ =>
                    {
                        return _mapper.Map<FundingLine>(_);
                    }).ToList();

                    // Set total funding
                    generatedProviderResult.TotalFunding = generatedProviderResult.FundingLines
                        .Sum(p => {
                            return p.Type == OrganisationGroupingReason.Payment ? p.Value : 0;
                        });

                    Dictionary<uint, GeneratorModels.Calculation> uniqueCalculations = new Dictionary<uint, GeneratorModels.Calculation>();

                    // Get calculations
                    IEnumerable<GeneratorModels.Calculation> fundingCalculations = uniqueFundingLine.Values?.SelectMany(_ => _.Calculations.Flatten(calc => calc.Calculations)) ?? new GeneratorModels.Calculation[0];

                    generatedProviderResult.Calculations = fundingCalculations.Where(_ => uniqueCalculations.TryAdd(_.TemplateCalculationId, _)).Select(_ =>
                    {
                        return _mapper.Map<FundingCalculation>(_);
                    }).ToList();

                    Dictionary<uint, GeneratorModels.ReferenceData> uniqueReferenceData = new Dictionary<uint, GeneratorModels.ReferenceData>();
                    
                    // Get reference data
                    IEnumerable<GeneratorModels.ReferenceData> fundingReferenceData = uniqueCalculations.Values?.SelectMany(_ => _.ReferenceData);

                    generatedProviderResult.ReferenceData = fundingReferenceData.Where(_ => uniqueReferenceData.TryAdd(_.TemplateReferenceId, _)).Select(_ =>
                    {
                        return _mapper.Map<FundingReferenceData>(_);
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
