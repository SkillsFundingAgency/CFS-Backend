using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DatasetVersionBuilder : TestEntityBuilder
    {
        private string _blobName;
        private int? _version;
        private string _comment;
        private DateTimeOffset? _date;
        private Reference _author;
        private Reference _fundingStream;
        private string _providerVersionId;
        private string _description;

        public DatasetVersionBuilder WithFundingStream(Reference fundingStream)
        {
            _fundingStream = fundingStream;

            return this;
        }
        
        public DatasetVersionBuilder WithBlobName(string blobName)
        {
            _blobName = blobName;

            return this;
        }

        public DatasetVersionBuilder WithProviderVersionId(string providerVersionId)
        {
            _providerVersionId = providerVersionId;

            return this;
        }

        public DatasetVersionBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public DatasetVersionBuilder WithComment(string comment)
        {
            _comment = comment;

            return this;
        }

        public DatasetVersionBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }
        public DatasetVersionBuilder WithDate(DateTimeOffset date)
        {
            _date = date;

            return this;
        }

        public DatasetVersionBuilder WithAuthor(Reference author)
        {
            _author = author;

            return this;
        }

        public DatasetVersion Build()
        {
            return new DatasetVersion
            {
                BlobName = _blobName ?? NewRandomString(),
                Version = _version.GetValueOrDefault(NewRandomNumberBetween(1, 99)),
                Comment = _comment ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                Date = _date.GetValueOrDefault(NewRandomDateTime()),
                Author = _author,
                FundingStream = _fundingStream,
                ProviderVersionId = _providerVersionId
            };
        }
    }
}