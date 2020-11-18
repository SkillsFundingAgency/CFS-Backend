using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    public class FundingTemplateContentsBuilder : TestEntityBuilder
    {
        private TemplateMetadataContents _metadata;
        private string _schemaVersion;
        private string _contents;

        public FundingTemplateContentsBuilder WithMetadata(TemplateMetadataContents metadata)
        {
            _metadata = metadata;

            return this;
        }

        public FundingTemplateContentsBuilder WithSchemaVersion(string schemaVersion)
        {
            _schemaVersion = schemaVersion;

            return this;
        }

        public FundingTemplateContentsBuilder WithTemplateFileContents(string contents)
        {
            _contents = contents;

            return this;
        }

        public FundingTemplateContents Build() =>
            new FundingTemplateContents
            {
                Metadata = _metadata,
                SchemaVersion = _schemaVersion ?? NewRandomString(),
                TemplateFileContents = _contents ?? NewRandomString()
            };
    }
}