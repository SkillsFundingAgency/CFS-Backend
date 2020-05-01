using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Azure.Search.Models;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedProviderTransactionBuilder : TestEntityBuilder
    {
        private PublishedProviderStatus? _status;
        private Reference _author;
        private DateTimeOffset? _date;
        private decimal? _totalFunding;
        private IEnumerable<FundingLine> _fundingLines;

        public PublishedProviderTransactionBuilder WithAuthor(Reference author)
        {
            _author = author;

            return this;
        }

        public PublishedProviderTransactionBuilder WithDate(DateTimeOffset date)
        {
            _date = date;

            return this;
        }

        public PublishedProviderTransactionBuilder WithPublishedProviderStatus(PublishedProviderStatus status)
        {
            _status = status;

            return this;
        }

        public PublishedProviderTransactionBuilder WithTotalFunding(decimal? totalFunding)
        {
            _totalFunding = totalFunding;

            return this;
        }

        public PublishedProviderTransactionBuilder WithFundingLines(params FundingLine[] fundingLines)
        {
            _fundingLines = fundingLines;

            return this;
        }

        public PublishedProviderTransaction Build()
        {
            return new PublishedProviderTransaction
            {
                Status = _status.GetValueOrDefault(NewRandomEnum<PublishedProviderStatus>()),
                Author = _author,
                Date = _date ?? this.NewRandomDateTime(),
                TotalFunding = _totalFunding,
                FundingLines = _fundingLines
            };
        }
    }
}
