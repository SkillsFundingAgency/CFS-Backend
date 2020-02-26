using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ProviderBuilder : TestEntityBuilder
    {
        private string _providerId;
        private string _status;
        private string _establishmentNumber;
        private string _localAuthorityName;
        private string _laCode;
        private string _urn;
        private string _ukprn;
        private string _providerType;
        private string _providerSubType;
        private string _name;

        public ProviderBuilder WithName(string name)
        {
            _name = name;

            return this;
        }
        
        public ProviderBuilder WithLACode(string laCode)
        {
            _laCode = laCode;

            return this;
        }

        public ProviderBuilder WithURN(string urn)
        {
            _urn = urn;

            return this;
        }

        public ProviderBuilder WithUKPRN(string ukprn)
        {
            _ukprn = ukprn;

            return this;
        }

        public ProviderBuilder WithProviderType(string providerType)
        {
            _providerType = providerType;

            return this;
        }

        public ProviderBuilder WithProviderSubType(string providerSubType)
        {
            _providerSubType = providerSubType;

            return this;
        }

        public ProviderBuilder WithLocalAuthorityName(string localAuthorityName)
        {
            _localAuthorityName = localAuthorityName;

            return this;
        }
        
        public ProviderBuilder WithEstablishmentNumber(string establishmentNumber)
        {
            _establishmentNumber = establishmentNumber;

            return this;
        }
        
        public ProviderBuilder WithStatus(string status)
        {
            _status = status;

            return this;
        }

        public ProviderBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public Provider Build()
        {
            return new Provider
            {
                ProviderId = _providerId ?? NewRandomString(),
                Authority = NewRandomString(),
                Name = _name ?? NewRandomString(),
                Postcode = NewRandomString(),
                Status = _status ?? NewRandomString(),
                Successor = NewRandomString(),
                Town = NewRandomString(),
                CountryCode = NewRandomString(),
                CountryName = NewRandomString(),
                DateClosed = NewRandomDateTime(),
                DateOpened = NewRandomDateTime(),
                DistrictCode = NewRandomString(),
                DistrictName = NewRandomString(),
                EstablishmentNumber = _establishmentNumber ?? NewRandomString(),
                LegalName = NewRandomString(),
                ProviderType = _providerType ?? NewRandomString(),
                ProviderSubType = _providerSubType ?? NewRandomString(),
                TrustCode = NewRandomString(),
                TrustStatus = NewRandomEnum<ProviderTrustStatus>(),
                TrustName = NewRandomString(),
                WardCode = NewRandomString(),
                WardName = NewRandomString(),
                CensusWardCode = NewRandomString(),
                CensusWardName = NewRandomString(),
                CompaniesHouseNumber = NewRandomString(),
                DfeEstablishmentNumber = NewRandomString(),
                GroupIdNumber = NewRandomString(),
                LACode = _laCode ?? NewRandomString(),
                LocalAuthorityName = _localAuthorityName ?? NewRandomString(),
                ParliamentaryConstituencyCode = NewRandomString(),
                ParliamentaryConstituencyName = NewRandomString(),
                RscRegionCode = NewRandomString(),
                RscRegionName = NewRandomString(),
                URN = _urn ?? NewRandomString(),
                UKPRN = _ukprn ?? NewRandomString(),
                GovernmentOfficeRegionCode = NewRandomString(),
                GovernmentOfficeRegionName = NewRandomString(),
            };
        }
    }
}