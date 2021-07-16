using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class DatasetSpecificationRelationshipViewModelBuilder : TestEntityBuilder
    {
        private string _datasetId;
        private DateTimeOffset? _lastUpdatedDate;
        private string _name;
        private PublishedSpecificationConfiguration _publishedSpecificationConfiguration;
        private string _id;

        public DatasetSpecificationRelationshipViewModelBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public DatasetSpecificationRelationshipViewModelBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public DatasetSpecificationRelationshipViewModelBuilder WithDatasetId(string datasetId)
        {
            _datasetId = datasetId;

            return this;
        }

        public DatasetSpecificationRelationshipViewModelBuilder WithLastUpdatedDate(DateTimeOffset lastUpdatedDate)
        {
            _lastUpdatedDate = lastUpdatedDate;

            return this;
        }

        public DatasetSpecificationRelationshipViewModelBuilder WithPublishedSpecificationConfiguration(PublishedSpecificationConfiguration publishedSpecificationConfiguration)
        {
            _publishedSpecificationConfiguration = publishedSpecificationConfiguration;

            return this;
        }

        public DatasetSpecificationRelationshipViewModel Build()
        {
            return new DatasetSpecificationRelationshipViewModel
            {
                DatasetId = _datasetId,
                LastUpdatedDate = _lastUpdatedDate,
                Id = _id,
                Name = _name,
                PublishedSpecificationConfiguration = _publishedSpecificationConfiguration
            };
        }
    }
}
