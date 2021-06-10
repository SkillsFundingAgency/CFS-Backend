using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.Interfaces;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using System;
using CalculateFunding.Services.Core.FeatureToggles;

namespace CalculateFunding.Services.Results.SearchIndex
{
    public class ProviderCalculationResultsIndexTransformer : ISearchIndexTrasformer<ProviderResult, ProviderCalculationResultsIndex>
    {
        private readonly IFeatureToggle _featureToggle;

        public ProviderCalculationResultsIndexTransformer(IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            _featureToggle = featureToggle;
        }

        public Task<ProviderCalculationResultsIndex> Transform(ProviderResult providerResult, ISearchIndexProcessorContext context)
        {
            Guard.ArgumentNotNull(providerResult, nameof(ProviderResult));

            var processorContext = context as ProviderCalculationResultsIndexProcessorContext;
            Guard.ArgumentNotNull(processorContext, nameof(ProviderCalculationResultsIndexProcessorContext));

            ProviderCalculationResultsIndex providerCalculationResultsIndex = new ProviderCalculationResultsIndex
            {
                SpecificationId = providerResult.SpecificationId,
                SpecificationName = processorContext.SpecificationName,
                ProviderId = providerResult.Provider?.Id,
                ProviderName = providerResult.Provider?.Name,
                ProviderType = providerResult.Provider?.ProviderType,
                ProviderSubType = providerResult.Provider?.ProviderSubType,
                LocalAuthority = providerResult.Provider?.Authority,
                LastUpdatedDate = DateTimeOffset.Now,
                UKPRN = providerResult.Provider?.UKPRN,
                URN = providerResult.Provider?.URN,
                UPIN = providerResult.Provider?.UPIN,
                EstablishmentNumber = providerResult.Provider?.EstablishmentNumber,
                OpenDate = providerResult.Provider?.DateOpened,
                IsIndicativeProvider = providerResult.IsIndicativeProvider,
                CalculationId = providerResult.CalculationResults.Select(m => m.Calculation.Id).ToArraySafe(),
                CalculationName = providerResult.CalculationResults.Select(m => m.Calculation.Name).ToArraySafe(),
                CalculationResult = providerResult.CalculationResults.Select(m => !string.IsNullOrEmpty(m.Value?.ToString()) ? m.Value.ToString() : "null").ToArraySafe(),
            };

            if (providerResult.FundingLineResults != null)
            {
                providerCalculationResultsIndex.FundingLineName = providerResult.FundingLineResults.Select(m => m.FundingLine.Name).ToArraySafe();
                providerCalculationResultsIndex.FundingLineFundingStreamId = providerResult.FundingLineResults.Select(m => m.FundingLineFundingStreamId).ToArraySafe();
                providerCalculationResultsIndex.FundingLineId = providerResult.FundingLineResults.Select(m => m.FundingLine.Id).ToArraySafe();
                providerCalculationResultsIndex.FundingLineResult = providerResult.FundingLineResults.Select(m => !string.IsNullOrEmpty(m.Value?.ToString()) ? m.Value.ToString() : "null").ToArraySafe();
            }

            if (_featureToggle.IsExceptionMessagesEnabled())
            {
                providerCalculationResultsIndex.CalculationException = providerResult.CalculationResults
                    .Where(m => !string.IsNullOrWhiteSpace(m.ExceptionType))
                    .Select(e => e.Calculation.Id)
                    .ToArraySafe();

                providerCalculationResultsIndex.CalculationExceptionType = providerResult.CalculationResults
                    .Select(m => m.ExceptionType ?? string.Empty)
                    .ToArraySafe();

                providerCalculationResultsIndex.CalculationExceptionMessage = providerResult.CalculationResults
                    .Select(m => m.ExceptionMessage ?? string.Empty)
                    .ToArraySafe();

                if (providerResult.FundingLineResults != null)
                {
                    providerCalculationResultsIndex.FundingLineException = providerResult.FundingLineResults
                    .Where(m => !string.IsNullOrWhiteSpace(m.ExceptionType))
                    .Select(e => e.FundingLine.Id)
                    .ToArraySafe();

                    providerCalculationResultsIndex.FundingLineExceptionType = providerResult.FundingLineResults
                        .Select(m => m.ExceptionType ?? string.Empty)
                        .ToArraySafe();

                    providerCalculationResultsIndex.FundingLineExceptionMessage = providerResult.FundingLineResults
                        .Select(m => m.ExceptionMessage ?? string.Empty)
                        .ToArraySafe();
                }
            }

            return Task.FromResult(providerCalculationResultsIndex);
        }
    }
}
