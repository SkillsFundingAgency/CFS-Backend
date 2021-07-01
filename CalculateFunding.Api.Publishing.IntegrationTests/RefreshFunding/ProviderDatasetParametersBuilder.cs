using System.Collections.Generic;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding
{
    public class ProviderDatasetParametersBuilder : TestEntityBuilder
    {
        private string _path;
        private IEnumerable<ProviderDatasetRowParameters> _rows;

        public ProviderDatasetParametersBuilder WithPath(string path)
        {
            _path = path;

            return this;
        }
        
        public ProviderDatasetParametersBuilder WithRows(params ProviderDatasetRowParameters[] rows)
        {
            _rows = rows;

            return this;
        }
        public ProviderDatasetParameters Build() =>
            new ProviderDatasetParameters
            {
                Path = _path ?? NewRandomString(),
                Rows = _rows ?? new ProviderDatasetRowParameters[0]
            };
    }
}