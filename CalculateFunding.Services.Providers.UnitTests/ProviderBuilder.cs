using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Models.Providers;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public class ProviderBuilder : TestEntityBuilder
    {
        private TrustStatus? _trustStatus;
        private string _providerId;

        public ProviderBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public ProviderBuilder WithTrustStatus(TrustStatus trustStatus)
        {
            _trustStatus = trustStatus;

            return this;
        }
        
        public Provider Build()
        {
            return new Provider
            {
                Name = NewRandomString(),
                ProviderId = _providerId ?? NewRandomString(),
                ProviderProfileIdType = NewRandomString(),
                UKPRN = NewRandomString(),
                URN = NewRandomString(),
                Authority = NewRandomString(),
                UPIN = NewRandomString(),
                ProviderSubType = NewRandomString(),
                EstablishmentNumber = NewRandomString(),
                ProviderType = NewRandomString(),
                DateOpened = NewRandomDateTime(),
                DateClosed = NewRandomDateTime(),
                LACode = NewRandomString(),
                CrmAccountId = NewRandomString(),
                LegalName = NewRandomString(),
                NavVendorNo = NewRandomString(),
                DfeEstablishmentNumber = NewRandomString(),
                Status = NewRandomString(),
                PhaseOfEducation = NewRandomString(),
                ReasonEstablishmentClosed = NewRandomString(),
                ReasonEstablishmentOpened = NewRandomString(),
                Successor = NewRandomString(),
                TrustStatus = _trustStatus.GetValueOrDefault(NewRandomEnum<TrustStatus>()),
                TrustName = NewRandomString(),
                TrustCode = NewRandomString(),
                CompaniesHouseNumber = NewRandomString(),
                GroupIdNumber = NewRandomString(),
                RscRegionName = NewRandomString(),
                RscRegionCode = NewRandomString(),
                GovernmentOfficeRegionName = NewRandomString(),
                GovernmentOfficeRegionCode = NewRandomString(),
                DistrictName = NewRandomString(),
                DistrictCode = NewRandomString(),
                WardName = NewRandomString(),
                WardCode = NewRandomString(),
                CensusWardName = NewRandomString(),
                CensusWardCode = NewRandomString(),
                MiddleSuperOutputAreaName = NewRandomString(),
                MiddleSuperOutputAreaCode = NewRandomString(),
                LowerSuperOutputAreaName = NewRandomString(),
                LowerSuperOutputAreaCode = NewRandomString(),
                ParliamentaryConstituencyName = NewRandomString(),
                ParliamentaryConstituencyCode = NewRandomString(),
                CountryCode = NewRandomString(),
                CountryName = NewRandomString(),
                LocalGovernmentGroupTypeCode = NewRandomString(),
                LocalGovernmentGroupTypeName = NewRandomString(),
                Street = NewRandomString(),
                Locality = NewRandomString(),
                Address3 = NewRandomString()
            };
        }
    }
}