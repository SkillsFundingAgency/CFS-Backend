using System;

namespace CalculateFunding.Models.External
{
    [Serializable]
    public class Provider
    {
        public Provider()
        {
        }

        public Provider(string ukprn, string upin, DateTime? providerOpenDate, string legalName)
        {
            Ukprn = ukprn;
            Upin = upin;
            ProviderOpenDate = providerOpenDate;
            LegalName = legalName;
        }

        public string Ukprn { get; set; }

        public string Upin { get; set; }

        public DateTimeOffset? ProviderOpenDate { get; set; }

        public string LegalName { get; set; }
    }
}