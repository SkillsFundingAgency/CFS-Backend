using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Datasets.Services
{
    public class GetDatasetBlobModelBuilder : TestEntityBuilder
    {
        private string _filename;
        private int? _version;
        private string _datasetId;
        private string _definitionId;
        private string _description;
        private string _comment;
        private string _fundingStreamId;
        private ReferenceBuilder _lastUpdatedBy;

        public GetDatasetBlobModelBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public GetDatasetBlobModelBuilder WithFileName(string filename)
        {
            _filename = filename;

            return this;
        }

        public GetDatasetBlobModelBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public GetDatasetBlobModelBuilder WithDatasetId(string datasetId)
        {
            _datasetId = datasetId;

            return this;
        }

        public GetDatasetBlobModelBuilder WithDefinitionId(string definitionId)
        {
            _definitionId = definitionId;

            return this;
        }

        public GetDatasetBlobModelBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public GetDatasetBlobModelBuilder WithComment(string comment)
        {
            _comment = comment;

            return this;
        }

        public GetDatasetBlobModelBuilder WithLastUpdatedBy(ReferenceBuilder lastUpdatedBy)
        {
            _lastUpdatedBy = lastUpdatedBy;

            return this;
        }

        public GetDatasetBlobModel Build()
        {
            _lastUpdatedBy = _lastUpdatedBy ?? new ReferenceBuilder();
            return new GetDatasetBlobModel
            {
                Filename = _filename ?? NewRandomString() + ".xslx",
                Version = _version.GetValueOrDefault(NewRandomNumberBetween(1, 99)),
                DatasetId = _datasetId ?? NewRandomString(),
                DefinitionId = _definitionId ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                Comment = _comment ?? NewRandomString(),
                LastUpdatedById = _lastUpdatedBy.Build().Id,
                LastUpdatedByName = _lastUpdatedBy.Build().Name,
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
            };
        }
    }
}
