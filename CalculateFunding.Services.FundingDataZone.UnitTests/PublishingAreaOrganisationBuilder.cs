using CalculateFunding.Services.FundingDataZone.SqlModels;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    public class PublishingAreaOrganisationBuilder : TestEntityBuilder
    {
        public PublishingAreaOrganisation Build()
        {
            return new PublishingAreaOrganisation
            {
                Name = NewRandomString(),
                Ukprn = NewRandomString(),
                Upin = NewRandomString(),
                Urn = NewRandomString(),
                LaCode = NewRandomString(),
                LaOrg = NewRandomString(),
                TrustCode = NewRandomString(),
                CompanyHouseNumber = NewRandomString(),
                PaymentOrganisationId = NewRandomNumberBetween(1, int.MaxValue),
                PaymentOrganisationType = NewRandomString(),
                ProviderSnapshotId = NewRandomNumberBetween(1, int.MaxValue)
            };
        }
    }
}