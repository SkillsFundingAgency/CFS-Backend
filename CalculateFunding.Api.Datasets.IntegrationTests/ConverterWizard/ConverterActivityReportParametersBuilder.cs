using System.Collections.Generic;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class ConverterActivityReportParametersBuilder : TestEntityBuilder
    {
        private string _name;
        private IEnumerable<ConverterActivityReportRowParameters> _rows;

        public ConverterActivityReportParametersBuilder WithName(string name)
        {
            _name = name;

            return this;
        }
        
        public ConverterActivityReportParametersBuilder WithRows(params ConverterActivityReportRowParameters[] rows)
        {
            _rows = rows;

            return this;
        }
        public ConverterActivityReportParameters Build() =>
            new ConverterActivityReportParameters
            {
                Name = _name ?? NewRandomString(),
                Rows = _rows ?? new ConverterActivityReportRowParameters[0]
            };
    }
}