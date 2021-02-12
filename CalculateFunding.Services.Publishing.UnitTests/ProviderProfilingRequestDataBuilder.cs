using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ProviderProfilingRequestDataBuilder : TestEntityBuilder
    {
        private string _providerType;
        private string _providerSubType;
        private PublishedProviderVersion _publishedProviderVersion;
        private IDictionary<string, string> _profilePatternKeys;
        private IEnumerable<FundingLine> _fundingLinesToProfile;
        private HashSet<string> _newInScopeFundingLines;

        public ProviderProfilingRequestDataBuilder WithProviderType(string providerType)
        {
            _providerType = providerType;

            return this;
        }

        public ProviderProfilingRequestDataBuilder WithProviderSubType(string providerSubType)
        {
            _providerSubType = providerSubType;

            return this;
        }

        public ProviderProfilingRequestDataBuilder WithPublishedProviderVersion(PublishedProviderVersion publishedProviderVersion)
        {
            _publishedProviderVersion = publishedProviderVersion;

            return this;
        }

        public ProviderProfilingRequestDataBuilder WithProfilePatternKeys(params (string key, string value)[] profilePatternKeys)
        {
            _profilePatternKeys = profilePatternKeys
                .ToDictionary(_ => _.key, _ => _.value);

            return this;
        }

        public ProviderProfilingRequestDataBuilder WithFundingLinesToProfile(params FundingLine[] fundingLines)
        {
            _fundingLinesToProfile = fundingLines;

            return this;
        }

        public ProviderProfilingRequestDataBuilder WithInScopeFundingLines(params FundingLine[] inScopeFundingLines)
        {
            _newInScopeFundingLines = inScopeFundingLines.Select(_ => _.FundingLineCode).ToHashSet();

            return this;
        }

        public ProviderProfilingRequestData Build()
        {
            return new ProviderProfilingRequestData
            {
                ProviderType = _providerType,
                ProviderSubType = _providerSubType,
                PublishedProvider = _publishedProviderVersion,
                ProfilePatternKeys = _profilePatternKeys,
                FundingLinesToProfile = _fundingLinesToProfile,
                NewInScopeFundingLines = _newInScopeFundingLines
            };
        }
    }
}