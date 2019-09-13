using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedFundingVersionBuilder : TestEntityBuilder
    {
        private string _fundingId;
        private IEnumerable<string> _providerFundings;
        private PublishedFundingPeriod _fundingPeriod;
        private string _fundingStreamId;
        private int? _version;
        private string _specificationId;
        private PublishedFundingStatus? _status;
        private OrganisationGroupTypeClassification? _organisationGroupTypeCategory;
        private OrganisationGroupTypeIdentifier? _organisationGroupTypeIdentifier;
        private OrganisationGroupTypeCode? _organisationGroupTypeCode;
        private string _organisationGroupIdentifierValue;
        private CalculateFunding.Models.Publishing.GroupingReason _groupingReason;
        private int? _majorVersion;
        private int _minorVersion;

        public PublishedFundingVersionBuilder WithFundingId(string fundingId)
        {
            _fundingId = fundingId;

            return this;
        }

        public PublishedFundingVersionBuilder WithPublishedProviderStatus(PublishedFundingStatus status)
        {
            _status = status;

            return this;
        }
        
        public PublishedFundingVersionBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public PublishedFundingVersionBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public PublishedFundingVersionBuilder WithFundingPeriod(PublishedFundingPeriod fundingPeriod)
        {
            _fundingPeriod = fundingPeriod;

            return this;
        }

        public PublishedFundingVersionBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public PublishedFundingVersionBuilder WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason groupingReason)
        {
            _groupingReason = groupingReason;

            return this;
        }

        public PublishedFundingVersionBuilder WithOrganisationGroupTypeCategory(OrganisationGroupTypeClassification organisationGroupTypeClassification)
        {
            _organisationGroupTypeCategory = organisationGroupTypeClassification;

            return this;
        }

        public PublishedFundingVersionBuilder WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier organisationGroupTypeIdentifier)
        {
            _organisationGroupTypeIdentifier = organisationGroupTypeIdentifier;

            return this;
        }

        public PublishedFundingVersionBuilder WithOrganisationGroupTypeCode(OrganisationGroupTypeCode organisationGroupTypeCode)
        {
            _organisationGroupTypeCode = organisationGroupTypeCode;

            return this;
        }

        public PublishedFundingVersionBuilder WithOrganisationGroupIdentifierValue(string organisationGroupIdentifierValue)
        {
            _organisationGroupIdentifierValue = organisationGroupIdentifierValue;

            return this;
        }

        public PublishedFundingVersionBuilder WithProviderFundings(IEnumerable<string> providerFundings)
        {
            _providerFundings = providerFundings;

            return this;
        }

        public PublishedFundingVersionBuilder WithMajor(int majorVersion)
        {
            _majorVersion = majorVersion;

            return this;
        }
        public PublishedFundingVersionBuilder WithMinor(int minorVersion)
        {
            _minorVersion = minorVersion;

            return this;
        }

        public PublishedFundingVersion Build()
        {
            return new PublishedFundingVersion
            {
                FundingId = _fundingId ?? NewRandomString(),
                ProviderFundings = _providerFundings,
                SpecificationId = _specificationId ?? NewRandomString(),
                FundingPeriod = _fundingPeriod,
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                Version = _version ?? 1,
                Status = _status.GetValueOrDefault(NewRandomEnum<PublishedFundingStatus>()),
                OrganisationGroupTypeCategory = _organisationGroupTypeCategory.GetValueOrDefault(NewRandomEnum<OrganisationGroupTypeClassification>()).ToString(),
                OrganisationGroupTypeIdentifier = _organisationGroupTypeIdentifier.GetValueOrDefault(NewRandomEnum<OrganisationGroupTypeIdentifier>()).ToString(),
                OrganisationGroupTypeCode = _organisationGroupTypeCode.GetValueOrDefault(NewRandomEnum<OrganisationGroupTypeCode>()).ToString(),
                OrganisationGroupIdentifierValue = _organisationGroupIdentifierValue,
                GroupingReason = _groupingReason,
                MajorVersion = _majorVersion ?? 1,
                MinorVersion = _minorVersion
            };
        }
    }
}
