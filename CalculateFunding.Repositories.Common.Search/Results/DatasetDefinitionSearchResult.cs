using System;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class DatasetDefinitionSearchResult
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string FundingStreamId { get; set; }

        public string FundingStreamName { get; set; }

        public string ProviderIdentifier { get; set; }

        public DateTimeOffset LastUpdatedDate { get; set; }

        public bool ConverterEnabled { get; set; }
    }
}
