using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Tests.Common.Helpers;
using Provider = CalculateFunding.Common.ApiClient.FundingDataZone.Models.Provider;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public class FundingDataZoneProviderBuilder : TestEntityBuilder
    {
        private string _providerId;

        public FundingDataZoneProviderBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public Provider Build()
        {
            return new Provider
            {
                Name = NewRandomString(),
                ProviderId = _providerId ?? NewRandomString(),
                UKPRN = NewRandomString(),
                URN = NewRandomString(),
                Authority = NewRandomString(),
                UPIN = NewRandomString(),
                ProviderSubType = NewRandomString(),
                EstablishmentNumber = NewRandomString(),
                ProviderType = NewRandomString(),
                LACode = NewRandomString(),
                LAOrg = NewRandomString(),
                CrmAccountId = NewRandomString(),
                LegalName = NewRandomString(),
                NavVendorNo = NewRandomString(),
                DfeEstablishmentNumber = NewRandomString(),
                Status = NewRandomString(),
                PhaseOfEducation = NewRandomString(),
                ReasonEstablishmentClosed = NewRandomString(),
                ReasonEstablishmentOpened = NewRandomString(),
                TrustStatus = NewRandomEnum<TrustStatus>().ToString(),
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
            };
        }
    }
}