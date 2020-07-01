using System;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class DatasetSpecificationRelationshipViewModelBuilder : TestEntityBuilder
    {
        private string _datasetId;
        private DateTimeOffset? _lastUpdatedDate;

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
        
        public DatasetSpecificationRelationshipViewModel Build()
        {
            return new DatasetSpecificationRelationshipViewModel
            {
                DatasetId = _datasetId,
                LastUpdatedDate = _lastUpdatedDate
            };
        }
    }
}