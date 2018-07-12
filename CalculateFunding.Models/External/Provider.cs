namespace CalculateFunding.Models.External
{
    public class Provider
    {
        public Provider(string ukprn, string upin, string providerOpenDate, string legalName)
        {
            Ukprn = ukprn;
            Upin = upin;
            ProviderOpenDate = providerOpenDate;
            LegalName = legalName;
        }

        public string Ukprn { get; }

        public string Upin { get; }

        public string ProviderOpenDate { get; }

        public string LegalName { get; }
    }
}