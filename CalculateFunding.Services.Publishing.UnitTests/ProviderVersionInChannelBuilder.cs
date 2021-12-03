using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ProviderVersionInChannelBuilder : TestEntityBuilder
    {
        private string _channelCode;
        private string _channelName;
        private string _providerId;
        private int? _channelId;
        private int? _majorVersion;
        private int? _minorVersion;

        public ProviderVersionInChannelBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;
            return this;
        }

        public ProviderVersionInChannelBuilder WithChannelCode(string channelCode)
        {
            _channelCode = channelCode;
            return this;
        }

        public ProviderVersionInChannelBuilder WithChannelName(string channelName)
        {
            _channelName = channelName;
            return this;
        }

        public ProviderVersionInChannelBuilder WithChannelId(int channelId)
        {
            _channelId = channelId;
            return this;
        }

        public ProviderVersionInChannelBuilder WithMajorVersion(int majorVersion)
        {
            _majorVersion = majorVersion;
            return this;
        }

        public ProviderVersionInChannelBuilder WithMinorVersion(int minorVersion)
        {
            _minorVersion = minorVersion;
            return this;
        }

        public ProviderVersionInChannel Build()
        {
            return new ProviderVersionInChannel
            {
                ChannelCode = _channelCode ?? NewRandomString(),
                ChannelId = _channelId ?? NewRandomNumberBetween(1, int.MaxValue),
                ChannelName = _channelName ?? NewRandomString(),
                MajorVersion = _majorVersion ?? NewRandomNumberBetween(1, int.MaxValue),
                MinorVersion = _minorVersion ?? NewRandomNumberBetween(1, int.MaxValue),
                ProviderId = _providerId ?? NewRandomString()
            };
        }
    }
}
