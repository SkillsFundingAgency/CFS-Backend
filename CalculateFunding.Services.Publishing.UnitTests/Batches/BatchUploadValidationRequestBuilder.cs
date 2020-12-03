using CalculateFunding.Services.Publishing.Batches;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Batches
{
    public class BatchUploadValidationRequestBuilder : TestEntityBuilder
    {
        private string _batchId;
        private string _fundingStreamId;
        private string _fundingPeriodId;

        public BatchUploadValidationRequestBuilder WithBatchId(string batchId)
        {
            _batchId = batchId;

            return this;
        }

        public BatchUploadValidationRequestBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public BatchUploadValidationRequestBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public BatchUploadValidationRequest Build() =>
            new BatchUploadValidationRequest
            {
                BatchId = _batchId,
                FundingPeriodId = _fundingPeriodId,
                FundingStreamId = _fundingStreamId
            };
    }
}