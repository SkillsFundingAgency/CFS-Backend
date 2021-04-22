using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class TemplateMetadataContentsBuilder : TestEntityBuilder
    {
        private IEnumerable<FundingLine> _fundingLines = Enumerable.Empty<FundingLine>();
        private string _schemaVersion;

        public TemplateMetadataContentsBuilder WithFundingLines(params FundingLine[] fundingLines)
        {
            _fundingLines = fundingLines;

            return this;
        }

        public TemplateMetadataContentsBuilder WithSchemeVersion(string schemaVersion)
        {
            _schemaVersion = schemaVersion;

            return this;
        }

        public TemplateMetadataContents Build()
        {
            return new TemplateMetadataContents
            {
                RootFundingLines = _fundingLines.ToArray(),
                SchemaVersion = _schemaVersion
            };
        }
    }
}