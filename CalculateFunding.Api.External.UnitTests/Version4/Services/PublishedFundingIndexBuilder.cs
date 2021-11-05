using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.External.UnitTests.Version4.Services
{
    public class PublishedFundingIndexBuilder : TestEntityBuilder
    {
        private string _documentPath;

        public PublishedFundingIndexBuilder WithDocumentPath(string documentPath)
        {
            _documentPath = documentPath;

            return this;
        }

        public PublishedFundingIndex Build()
        {
            return new PublishedFundingIndex
            {
                DocumentPath = _documentPath ?? NewRandomString()
            };
        }
    }
}