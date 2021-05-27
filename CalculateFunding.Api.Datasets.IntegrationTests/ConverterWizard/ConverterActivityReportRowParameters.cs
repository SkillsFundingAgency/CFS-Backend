using System.Collections.Generic;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class ConverterActivityReportRowParameters
    {
        public string TargetUKPRN { get; set; }
        
        public string TargetProviderName { get; set; }
        
        public string TargetProviderStatus { get; set; }
        
        public string TargetOpeningDate { get; set; }
        
        public string TargetProviderIneligible { get; set; }

        public string SourceProviderUKPRN { get; set; }
    }
}