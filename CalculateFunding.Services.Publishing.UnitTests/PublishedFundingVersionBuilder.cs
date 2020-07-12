using System;
using System.Collections.Generic;
using CalculateFunding.Common.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

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
        private OrganisationGroupTypeClassification? _organisationGroupTypeClassification;
        private OrganisationGroupTypeIdentifier? _organisationGroupTypeIdentifier;
        private OrganisationGroupTypeCode? _organisationGroupTypeCode;
        private string _organisationGroupIdentifierValue;
        private CalculateFunding.Models.Publishing.GroupingReason _groupingReason;
        private int? _majorVersion;
        private int _minorVersion;
        private string _organisationGroupName;
        private Reference _author;
        private DateTimeOffset _date;
        private IEnumerable<FundingLine> _fundingLines;
        private IEnumerable<VariationReason> _variationReasons;
        private decimal? _totalFunding;
        private DateTime _statusChangedDate;


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

        public PublishedFundingVersionBuilder WithAuthor(Reference author)
        {
            _author = author;

            return this;
        }

        public PublishedFundingVersionBuilder WithTotalFunding(decimal? totalFunding)
        {
            _totalFunding = totalFunding;

            return this;
        }

        public PublishedFundingVersionBuilder WithStatusChangedDate(DateTime statusChangedDate)
        {
            _statusChangedDate = statusChangedDate;

            return this;
        }

        public PublishedFundingVersionBuilder WithDate(string dateLiteral)
        {
            _date = DateTimeOffset.Parse(dateLiteral);

            return this;
        }

        public PublishedFundingVersionBuilder WithFundingLines(IEnumerable<FundingLine> fundingLines)
        {
            _fundingLines = fundingLines;

            return this;
        }

        public PublishedFundingVersionBuilder WithVariationReasons(IEnumerable<VariationReason> variationReasons)
        {
            _variationReasons = variationReasons;

            return this;
        }

        public PublishedFundingVersionBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public PublishedFundingVersionBuilder WithOrganisationGroupName(string organisationGroupName)
        {
            _organisationGroupName = organisationGroupName;

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

        public PublishedFundingVersionBuilder WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification organisationGroupTypeClassification)
        {
            _organisationGroupTypeClassification = organisationGroupTypeClassification;

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
                OrganisationGroupTypeClassification = _organisationGroupTypeClassification.GetValueOrDefault(NewRandomEnum<OrganisationGroupTypeClassification>()).ToString(),
                OrganisationGroupTypeIdentifier = _organisationGroupTypeIdentifier.GetValueOrDefault(NewRandomEnum<OrganisationGroupTypeIdentifier>()).ToString(),
                OrganisationGroupTypeCode = _organisationGroupTypeCode.GetValueOrDefault(NewRandomEnum<OrganisationGroupTypeCode>()).ToString(),
                OrganisationGroupIdentifierValue = _organisationGroupIdentifierValue,
                GroupingReason = _groupingReason,
                MajorVersion = _majorVersion ?? 1,
                MinorVersion = _minorVersion,
                OrganisationGroupName = _organisationGroupName,
                Author = _author,
                Date = _date,
                FundingLines = _fundingLines,
                VariationReasons = _variationReasons,
                TotalFunding =_totalFunding,
                StatusChangedDate = _statusChangedDate
            };
        }
    }
}
