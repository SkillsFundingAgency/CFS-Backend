using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Tests.Common.Helpers;
using Provider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using PublishingProvider = CalculateFunding.Models.Publishing.Provider;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ApiProviderBuilder : TestEntityBuilder
    {
        private string _providerId;
        private string _status;
        private string _successor;
        private PublishingProvider _copyFrom;

        public ApiProviderBuilder WithSuccessor(string successor)
        {
            _successor = successor;

            return this;
        }
        
        public ApiProviderBuilder WithPropertiesFrom(PublishingProvider provider)
        {
            _copyFrom = provider;

            return this;
        }
        
        public ApiProviderBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public ApiProviderBuilder WithStatus(string status)
        {
            _status = status;

            return this;
        }
        
        public Provider Build()
        {
            if (_copyFrom != null)
            {
                return new Provider
                {
                    ProviderId = _copyFrom.ProviderId,
                    Status = _copyFrom.Status,
                    Authority = _copyFrom.Authority,
                    Name = _copyFrom.Name,
                    Postcode = _copyFrom.Postcode,
                    Successor = _copyFrom.Successor,
                    Town = _copyFrom.Town,
                    CountryCode = _copyFrom.CountryCode,
                    CountryName = _copyFrom.CountryName,
                    DateClosed = _copyFrom.DateClosed,
                    DateOpened = _copyFrom.DateOpened,
                    DistrictCode = _copyFrom.DistrictCode,
                    DistrictName = _copyFrom.DistrictName,
                    EstablishmentNumber = _copyFrom.EstablishmentNumber,
                    LegalName = _copyFrom.LegalName,
                    ProviderType = _copyFrom.ProviderType,
                    TrustCode = _copyFrom.TrustCode,
                    TrustStatus = (TrustStatus)_copyFrom.TrustStatus,
                    TrustName = _copyFrom.TrustName,
                    WardCode = _copyFrom.WardCode,
                    WardName = _copyFrom.WardName,
                    CensusWardCode = _copyFrom.CensusWardCode,
                    CensusWardName = _copyFrom.CensusWardName,
                    CompaniesHouseNumber = _copyFrom.CompaniesHouseNumber,
                    DfeEstablishmentNumber = _copyFrom.DfeEstablishmentNumber,
                    GroupIdNumber = _copyFrom.GroupIdNumber,
                    LACode = _copyFrom.LACode,
                    LocalAuthorityName = _copyFrom.LocalAuthorityName,
                    ParliamentaryConstituencyCode = _copyFrom.ParliamentaryConstituencyCode,
                    ParliamentaryConstituencyName = _copyFrom.ParliamentaryConstituencyName,
                    RscRegionCode = _copyFrom.RscRegionCode,
                    RscRegionName = _copyFrom.RscRegionName,
                    URN = _copyFrom.URN,
                    GovernmentOfficeRegionCode = _copyFrom.GovernmentOfficeRegionCode,
                    GovernmentOfficeRegionName = _copyFrom.GovernmentOfficeRegionName
                };
            }
            
            return new Provider
            {
                ProviderId = _providerId ?? NewRandomString(),
                Status = _status ?? NewRandomString(),
                Authority = NewRandomString(),
                Name = NewRandomString(),
                Postcode = NewRandomString(),
                Successor = _successor,
                Town = NewRandomString(),
                CountryCode = NewRandomString(),
                CountryName = NewRandomString(),
                DateClosed = NewRandomDateTime(),
                DateOpened = NewRandomDateTime(),
                DistrictCode = NewRandomString(),
                DistrictName = NewRandomString(),
                EstablishmentNumber = NewRandomString(),
                LegalName = NewRandomString(),
                ProviderType = NewRandomString(),
                TrustCode = NewRandomString(),
                TrustStatus = NewRandomEnum<TrustStatus>(),
                TrustName = NewRandomString(),
                WardCode = NewRandomString(),
                WardName = NewRandomString(),
                CensusWardCode = NewRandomString(),
                CensusWardName = NewRandomString(),
                CompaniesHouseNumber = NewRandomString(),
                DfeEstablishmentNumber = NewRandomString(),
                GroupIdNumber = NewRandomString(),
                LACode = NewRandomString(),
                LocalAuthorityName = NewRandomString(),
                ParliamentaryConstituencyCode = NewRandomString(),
                ParliamentaryConstituencyName = NewRandomString(),
                RscRegionCode = NewRandomString(),
                RscRegionName = NewRandomString(),
                URN = NewRandomString(),
                GovernmentOfficeRegionCode = NewRandomString(),
                GovernmentOfficeRegionName = NewRandomString()
            };
        }
    }
}