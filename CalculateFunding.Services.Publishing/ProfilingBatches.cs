using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public class ProfilingBatches : IEnumerable<ProfilingBatch>
    {
        private IEnumerable<ProfilingBatch> _batches;
        
        public ProfilingBatches(IEnumerable<ProviderProfilingRequestData> publishedProviders)
        {
            BatchPublishedProviders(publishedProviders);
        }

        private void BatchPublishedProviders(IEnumerable<ProviderProfilingRequestData> publishedProviders)
        {
            (ProviderProfilingRequestData provider, FundingLine fundingLine)[] fundingLineBreakDown = GetFundingLinesBreakdown(publishedProviders).ToArray();

            _batches = fundingLineBreakDown.GroupBy(_ =>
                    new
                    {
                        _.provider.PublishedProvider.FundingStreamId,
                        _.provider.PublishedProvider.FundingPeriodId,
                        _.provider.ProviderType,
                        _.provider.ProviderSubType,
                        _.fundingLine.FundingLineCode,
                        ProfilePatternKey = _.provider.GetProfilePatternKey(_.fundingLine),
                        _.fundingLine.Value
                    })
                .Select(_ => new ProfilingBatch
                {
                    FundingLineCode = _.Key.FundingLineCode,
                    FundingLines = _.Select(group => group.fundingLine)
                        .ToArray(),
                    ProviderType = _.Key.ProviderType,
                    ProviderSubType = _.Key.ProviderSubType,
                    FundingPeriodId = _.Key.FundingPeriodId,
                    FundingStreamId = _.Key.FundingStreamId,
                    FundingValue = _.First().fundingLine.Value.GetValueOrDefault(),
                    PublishedProviders = _.Select(group => group.provider.PublishedProvider)
                        .ToArray(),
                    ProfilePatternKey = _.Key.ProfilePatternKey
                })
                .ToArray();
        }

        private IEnumerable<(ProviderProfilingRequestData, FundingLine)> GetFundingLinesBreakdown(
            IEnumerable<ProviderProfilingRequestData> providerData)
        {
            foreach (ProviderProfilingRequestData profilingRequestData in providerData)
            {
                foreach (FundingLine fundingLine in profilingRequestData.FundingLinesToProfile)
                {
                    yield return (profilingRequestData, fundingLine);
                }
            }
        }

        public IEnumerator<ProfilingBatch> GetEnumerator() => _batches.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}