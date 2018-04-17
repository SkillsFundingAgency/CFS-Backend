using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class TestScenarioSearchResult
    {
        public string Id { get; set; }

        public string TestResult { get; set; }
     
        public string SpecificationId { get; set; }

        public string SpecificationName { get; set; }

        public string TestScenarioId { get; set; }

        public string TestScenarioName { get; set; }

        public string ProviderId { get; set; }

        public string ProviderName { get; set; }

        public DateTimeOffset LastUpdatedDate { get; set; }

        public string ProviderType { get; set; }

        public string ProviderSubType { get; set; }

        public string LocalAuthority { get; set; }

        public string UKPRN { get; set; }

        public string URN { get; set; }

        public string UPIN { get; set; }

        public string EstablishmentNumber { get; set; }

        public string OpenDate { get; set; }
    }
}
