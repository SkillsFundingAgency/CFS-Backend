using System;

namespace CalculateFunding.Api.External.V1.Models
{
	/// <summary>
	/// Fields are made available only if it is provided from the provider datasource
	/// </summary>
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

        public string LANo { get; set; }

        public string LAEstablishmentNo { get; set; }
    }
}