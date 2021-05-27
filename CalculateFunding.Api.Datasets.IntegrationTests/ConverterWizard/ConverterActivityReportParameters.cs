using System.Collections.Generic;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class ConverterActivityReportParameters
    {
        public string Path => $"{Name}.csv";

        public string Name { get; set; }
        
        public IEnumerable<ConverterActivityReportRowParameters> Rows { get; set; }
    }
}