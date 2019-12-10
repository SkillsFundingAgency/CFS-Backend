using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ProviderSourceDatasetVersionBuilder : TestEntityBuilder
    {
        private IEnumerable<(string, object)> _rows;
        private string _providerId;
        private VersionReference _dataset;

        public ProviderSourceDatasetVersionBuilder WithRows(params (string, object)[] rows)
        {
            _rows = rows;

            return this;
        }

        public ProviderSourceDatasetVersionBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public ProviderSourceDatasetVersionBuilder WithDataset(VersionReference dataset)
        {
            _dataset = dataset;

            return this;
        }

        public ProviderSourceDatasetVersion Build()
        {
            return new ProviderSourceDatasetVersion
            {
                ProviderId = _providerId ?? NewRandomString(),
                Dataset = _dataset,
                Rows = _rows != null
                    ? new List<Dictionary<string, object>>(new[] {_rows.ToDictionary(_ => _.Item1, _ => _.Item2)})
                    : new List<Dictionary<string, object>>()
            };
        }
    }
}