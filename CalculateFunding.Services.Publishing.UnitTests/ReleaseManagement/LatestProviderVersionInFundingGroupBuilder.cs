using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    public class LatestProviderVersionInFundingGroupBuilder : TestEntityBuilder
    {
        private string _providerId;
        private int? _majorVersion;
        private string _organisationGroupTypeCode;
        private string _organisationGroupIdentifierValue;
        private string _organisationGroupName;
        private string _organisationGroupTypeClassification;
        private string _groupingReasonCode;

        public LatestProviderVersionInFundingGroupBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public LatestProviderVersionInFundingGroupBuilder WithMajorVersion(int majorVersion)
        {
            _majorVersion = majorVersion;

            return this;
        }

        public LatestProviderVersionInFundingGroupBuilder WithOrganisationGroupTypeCode(string organisationGroupTypeCode)
        {
            _organisationGroupTypeCode = organisationGroupTypeCode;

            return this;
        }

        public LatestProviderVersionInFundingGroupBuilder WithOrganisationGroupIdentifierValue(string organisationGroupIdentifierValue)
        {
            _organisationGroupIdentifierValue = organisationGroupIdentifierValue;

            return this;
        }

        public LatestProviderVersionInFundingGroupBuilder WithOrganisationGroupName(string organisationGroupName)
        {
            _organisationGroupName = organisationGroupName;

            return this;
        }

        public LatestProviderVersionInFundingGroupBuilder WithOrganisationGroupTypeClassification(string organisationGroupTypeClassification)
        {
            _organisationGroupTypeClassification = organisationGroupTypeClassification;

            return this;
        }

        public LatestProviderVersionInFundingGroupBuilder WithGroupingReasonCode(string groupingReasonCode)
        {
            _groupingReasonCode = groupingReasonCode;

            return this;
        }

        public LatestProviderVersionInFundingGroup Build()
            => new LatestProviderVersionInFundingGroup
            {
                ProviderId = _providerId ?? NewRandomString(),
                MajorVersion = _majorVersion ?? NewRandomNumberBetween(1, 1000),
                OrganisationGroupIdentifierValue = _organisationGroupIdentifierValue ?? NewRandomString(),
                OrganisationGroupName = _organisationGroupName ?? NewRandomString(),
                OrganisationGroupTypeClassification = _organisationGroupTypeClassification ?? NewRandomString(),
                GroupingReasonCode = _groupingReasonCode ?? NewRandomString(),
                OrganisationGroupTypeCode = _organisationGroupTypeCode ?? NewRandomString()
            };
    }
}
