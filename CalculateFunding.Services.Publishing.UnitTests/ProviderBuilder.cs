using System;
using System.Collections.Generic;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Azure.Search.Models;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ProviderBuilder : TestEntityBuilder
    {
        private string _providerVersionId;
        private string _providerId;
        private string _status;
        private string _establishmentNumber;
        private string _authority;
        private string _laCode;
        private string _urn;
        private string _ukprn;
        private string _providerType;
        private string _providerSubType;
        private string _name;
        private Provider _copyFrom;
        private string _successor;
        private DateTimeOffset? _dateClosed;
        private DateTimeOffset? _dateOpened;
        private string _reasonEstablishmentOpened;
        private string _trustCode;
        private string _trustName;
        private string _reasonEstablishmentClosed;
        private string _paymentOrganisationIdentifier;
        private IEnumerable<string> _successors;
        private ProviderTrustStatus? _trustStatus;

        public ProviderBuilder WithSuccessors(params string[] successors)
        {
            _successors = successors;

            return this;
        }
        
        public ProviderBuilder WithPaymentOrganisationIdentifier(string paymentOrganisationIdentifier)
        {
            _paymentOrganisationIdentifier = paymentOrganisationIdentifier;

            return this;
        }

        public ProviderBuilder WithReasonEstablishmentClosed(string reasonEstablishmentClosed)
        {
            _reasonEstablishmentClosed = reasonEstablishmentClosed;

            return this;
        }

        public ProviderBuilder WithTrustCode(string trustCode)
        {
            _trustCode = trustCode;

            return this;
        }

        public ProviderBuilder WithTrustStatus(ProviderTrustStatus trustStatus)
        {
            _trustStatus = trustStatus;

            return this;
        }

        public ProviderBuilder WithSuccessor(string successor)
        {
            _successor = successor;

            return this;
        }

        public ProviderBuilder WithPropertiesFrom(Provider provider)
        {
            _copyFrom = provider;

            return this;
        }

        public ProviderBuilder WithTrustName(string trustName)
        {
            _trustName = trustName;

            return this;
        }

        public ProviderBuilder WithReasonEstablishmentOpened(string reasonEstablishmentOpened)
        {
            _reasonEstablishmentOpened = reasonEstablishmentOpened;

            return this;
        }

        public ProviderBuilder WithDateOpened(DateTimeOffset? dateOpened)
        {
            _dateOpened = dateOpened;

            return this;
        }

        public ProviderBuilder WithDateClosed(DateTimeOffset? dateClosed)
        {
            _dateClosed = dateClosed;

            return this;
        }

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

        public ProviderBuilder WithAuthority(string authority)
        {
            _authority = authority;

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

        public ProviderBuilder WithProviderVersionId(string providerVersionId)
        {
            _providerVersionId = providerVersionId;

            return this;
        }

        public Provider Build() =>
            _copyFrom?.DeepCopy() ?? new Provider
            {
                ProviderVersionId = _providerVersionId ?? NewRandomString(),
                ProviderId = _providerId ?? NewRandomString(),
                Authority = _authority ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Postcode = NewRandomString(),
                Status = _status ?? NewRandomString(),
                Successor = _successor,
                Successors = _successors,
                Town = NewRandomString(),
                CountryCode = NewRandomString(),
                CountryName = NewRandomString(),
                DateClosed = _dateClosed,
                DateOpened = _dateOpened,
                DistrictCode = NewRandomString(),
                DistrictName = NewRandomString(),
                EstablishmentNumber = _establishmentNumber ?? NewRandomString(),
                LegalName = NewRandomString(),
                ProviderType = _providerType ?? NewRandomString(),
                ProviderSubType = _providerSubType ?? NewRandomString(),
                TrustCode = _trustCode ?? NewRandomString(),
                TrustStatus = _trustStatus.GetValueOrDefault(NewRandomEnum<ProviderTrustStatus>()),
                TrustName = _trustName ?? NewRandomString(),
                WardCode = NewRandomString(),
                WardName = NewRandomString(),
                CensusWardCode = NewRandomString(),
                CensusWardName = NewRandomString(),
                CompaniesHouseNumber = NewRandomString(),
                DfeEstablishmentNumber = NewRandomString(),
                GroupIdNumber = NewRandomString(),
                LACode = _laCode ?? NewRandomString(),
                ParliamentaryConstituencyCode = NewRandomString(),
                ParliamentaryConstituencyName = NewRandomString(),
                RscRegionCode = NewRandomString(),
                RscRegionName = NewRandomString(),
                URN = _urn ?? NewRandomString(),
                UKPRN = _ukprn ?? NewRandomString(),
                GovernmentOfficeRegionCode = NewRandomString(),
                GovernmentOfficeRegionName = NewRandomString(),
                ReasonEstablishmentOpened = _reasonEstablishmentOpened,
                ReasonEstablishmentClosed = _reasonEstablishmentClosed,
                PaymentOrganisationIdentifier = _paymentOrganisationIdentifier ?? NewRandomString(),
                PaymentOrganisationName = NewRandomString(),
            };
    }
}