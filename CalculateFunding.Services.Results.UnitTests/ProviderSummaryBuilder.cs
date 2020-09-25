using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class ProviderSummaryBuilder : TestEntityBuilder
    {
        private string _id;
        private string _ukPrn;
        private string _urn;
        private string _establishmentNumber;
        private string _name;
        private string _laCode;
        private string _authority;
        private string _providerType;
        private string _providerSubType;

        public ProviderSummaryBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public ProviderSummaryBuilder WithUKPRN(string ukPrn)
        {
            _ukPrn = ukPrn;

            return this;
        }

        public ProviderSummaryBuilder WithURN(string urn)
        {
            _urn = urn;

            return this;
        }

        public ProviderSummaryBuilder WithEstablishmentNumber(string establishmentNumber)
        {
            _establishmentNumber = establishmentNumber;

            return this;
        }

        public ProviderSummaryBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public ProviderSummaryBuilder WithLACode(string laCode)
        {
            _laCode = laCode;

            return this;
        }


        public ProviderSummaryBuilder WithAuthority(string authority)
        {
            _authority = authority;

            return this;
        }
        
        public ProviderSummaryBuilder WithLocalProviderType(string providerType)
        {
            _providerType = providerType;

            return this;
        }
        
        public ProviderSummaryBuilder WithLocalProviderSubType(string providerSubType)
        {
            _providerSubType = providerSubType;

            return this;
        }
        
        public ProviderSummary Build()
        {
            return new ProviderSummary
            {
                Authority = _authority ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                URN = _urn ?? NewRandomString(),
                UKPRN = _ukPrn ?? NewRandomString(),
                ProviderType = _providerType ?? NewRandomString(),
                ProviderSubType = _providerSubType ?? NewRandomString(),
                EstablishmentNumber = _establishmentNumber ?? NewRandomString(),
                LACode = _laCode ?? NewRandomString(),
                Id = _id ?? NewRandomString()
                // LocalAuthorityName = _localAuthorityName ?? NewRandomString()
            };
        }
    }
}