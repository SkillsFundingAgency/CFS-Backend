using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;
using TemplateMetadataDistinctFundingLinesContents = CalculateFunding.Common.ApiClient.Policies.Models.TemplateMetadataDistinctFundingLinesContents;
using TemplateMetadataFundingLine = CalculateFunding.Common.ApiClient.Policies.Models.TemplateMetadataFundingLine;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class TemplateMetadataDistinctFundingLinesContentsBuilder 
        : TestEntityBuilder
    {
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _templateVersion;
        private IEnumerable<TemplateMetadataFundingLine> _templateMetadataFundingLines;

        public TemplateMetadataDistinctFundingLinesContentsBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public TemplateMetadataDistinctFundingLinesContentsBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public TemplateMetadataDistinctFundingLinesContentsBuilder WithTemplateVersion(string templateVersion)
        {
            _templateVersion = templateVersion;

            return this;
        }

        public TemplateMetadataDistinctFundingLinesContentsBuilder WithTemplateMetadataFundingLines(params TemplateMetadataFundingLine[] templateMetadataFundingLines)
        {
            _templateMetadataFundingLines = templateMetadataFundingLines;

            return this;
        }

        public TemplateMetadataDistinctFundingLinesContents Build()
        {
            return new TemplateMetadataDistinctFundingLinesContents
            {
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                TemplateVersion = _templateVersion ?? NewRandomString(),
                FundingLines = _templateMetadataFundingLines
            };
        }
    }
}
