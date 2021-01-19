using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;

namespace CalculateFunding.Services.Publishing
{
    public class BatchProfilingRequestModels : IEnumerable<BatchProfilingRequestModel>
    {
        private IEnumerable<BatchProfilingRequestModel> _batchRequests;

        public BatchProfilingRequestModels(IEnumerable<ProfilingBatch> batches, int batchSize)
        {
            CreateBatchProfilingRequests(batches, batchSize);    
        }

        private void CreateBatchProfilingRequests(IEnumerable<ProfilingBatch> batches,
            int batchSize)
        {
            _batchRequests = batches.GroupBy(_ => new
                {
                    _.FundingPeriodId,
                    _.FundingStreamId,
                    _.ProfilePatternKey,
                    _.ProviderSubType,
                    _.ProviderType,
                    _.FundingLineCode,
                }).Select(_ => new BatchProfilingRequestModel
                {
                    ProviderType = _.Key.ProviderType,
                    ProviderSubType = _.Key.ProviderSubType,
                    FundingLineCode = _.Key.FundingLineCode,
                    FundingPeriodId = _.Key.FundingPeriodId,
                    FundingStreamId = _.Key.FundingStreamId,
                    ProfilePatternKey = _.Key.ProfilePatternKey,
                    FundingValues = _.Select(group => group.FundingValue)
                        .ToArray()
                })
                .SelectMany(_ => SplitFundingLinesByBatchSize(_, batchSize))
                .ToArray();
        }

        private IEnumerable<BatchProfilingRequestModel> SplitFundingLinesByBatchSize(BatchProfilingRequestModel requestModel,
            int batchSize)
            => BatchFundingValues(requestModel.FundingValues, batchSize)
                .Select(_ => new BatchProfilingRequestModel
                {
                    ProviderType = requestModel.ProviderType,
                    ProviderSubType = requestModel.ProviderSubType,
                    FundingLineCode = requestModel.FundingLineCode,
                    FundingPeriodId = requestModel.FundingPeriodId,
                    FundingStreamId = requestModel.FundingStreamId,
                    ProfilePatternKey = requestModel.ProfilePatternKey,
                    FundingValues = _
                    
                })
                .ToArray();

        private static IEnumerable<IEnumerable<decimal>> BatchFundingValues(IEnumerable<decimal> fundingValues,
            int batchSize)
        {
            decimal[] batch = null;
            int count = 0;

            foreach (decimal fundingValue in fundingValues)
            {
                batch ??= new decimal[batchSize];

                batch[count++] = fundingValue;

                if (count != batchSize)
                {
                    continue;
                }

                yield return batch;

                batch = null;
                count = 0;
            }

            if (batch != null && count > 0)
            {
                yield return batch.Take(count).ToArray();
            }
        }

        public IEnumerator<BatchProfilingRequestModel> GetEnumerator() 
            => _batchRequests.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}