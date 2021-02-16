using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class TemplateMetadataDistinctContentsBuilder : TestEntityBuilder
    {
        private string _templateVersion;
        private IEnumerable<TemplateMetadataFundingLine> _fundingLines;
        private IEnumerable<TemplateMetadataCalculation> _calculations;

        public TemplateMetadataDistinctContentsBuilder WithTemplateVerison(string templateVersion)
        {
            _templateVersion = templateVersion;
            return this;
        }

        public TemplateMetadataDistinctContentsBuilder WithFundingLines(IEnumerable<TemplateMetadataFundingLine> fundingLines)
        {
            _fundingLines = fundingLines;
            return this;
        }

        public TemplateMetadataDistinctContentsBuilder WithCalculations(IEnumerable<TemplateMetadataCalculation> calculations)
        {
            _calculations = calculations;
            return this;
        }

        public TemplateMetadataDistinctContents Build()
        {
            return new TemplateMetadataDistinctContents()
            {
                TemplateVersion = _templateVersion ?? NewRandomString(),
                FundingLines = _fundingLines ?? new List<TemplateMetadataFundingLine>(),
                Calculations = _calculations ?? new List<TemplateMetadataCalculation>()
            };
        }
    }
}