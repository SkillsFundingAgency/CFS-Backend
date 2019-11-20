using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedProviderFundingStreamStatusBuilder : TestEntityBuilder
    {
        private int _count;
        private string _fundingStreamId;
        private string _status;
        private decimal? _totalFunding;

        public PublishedProviderFundingStreamStatusBuilder WithCount(int count)
        {
            _count = count;
            return this;
        }

        public PublishedProviderFundingStreamStatusBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;
            return this;
        }

        public PublishedProviderFundingStreamStatusBuilder WithStatus(string status)
        {
            _status = status;
            return this;
        }

        public PublishedProviderFundingStreamStatusBuilder WithTotalFunding(decimal? totalFunding)
        {
            _totalFunding = totalFunding;
            return this;
        }

        public PublishedProviderFundingStreamStatus Build()
        {
            return new PublishedProviderFundingStreamStatus
            {
                Count = _count,
                FundingStreamId = _fundingStreamId,
                Status = _status,
                TotalFunding = _totalFunding
            };
        }
    }
}
