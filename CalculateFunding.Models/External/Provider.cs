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
            UKPRN = ukprn;
            UPIN = upin;
            ProviderOpenDate = providerOpenDate;
            LegalName = legalName;
        }

        public string UKPRN { get; set; }

        public string UPIN { get; set; }

        public DateTime? ProviderOpenDate { get; set; }

        public string LegalName { get; set; }
    }
}