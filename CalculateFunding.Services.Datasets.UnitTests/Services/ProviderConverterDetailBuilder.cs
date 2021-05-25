using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ProviderConverterDetailBuilder : TestEntityBuilder
    {
        private string _targetProviderName;
        private string _targetProviderId;
        private string _previousProviderIdentifier;
        private string _providerInEligible;
        private DateTimeOffset? _targetOpeningDate;
        private string _targetStatus;

        public ProviderConverterDetailBuilder WithTargetProviderId(string targetProviderId)
        {
            _targetProviderId = targetProviderId;

            return this;
        }

        public ProviderConverterDetailBuilder WithPreviousProviderIdentifier(string previousProviderIdentifier)
        {
            _previousProviderIdentifier = previousProviderIdentifier;

            return this;
        }

        public ProviderConverterDetailBuilder WithTargetProviderName(string targetProviderName)
        {
            _targetProviderName = targetProviderName;

            return this;
        }

        public ProviderConverterDetailBuilder WithProviderInEligible(string providerInEligible)
        {
            _providerInEligible = providerInEligible;

            return this;
        }

        public ProviderConverterDetailBuilder WithTargetOpeningDate(DateTimeOffset targetOpeningDate)
        {
            _targetOpeningDate = targetOpeningDate;

            return this;
        }

        public ProviderConverterDetailBuilder WithTargetStatus(string targetStatus)
        {
            _targetStatus = targetStatus;

            return this;
        }

        public ProviderConverterDetail Build()
        {
            return new ProviderConverterDetail
            {
                TargetProviderId = _targetProviderId ?? NewRandomString(),
                TargetProviderName = _targetProviderName ?? NewRandomString(),
                PreviousProviderIdentifier = _previousProviderIdentifier ?? NewRandomString(),
                ProviderInEligible = _providerInEligible,
                TargetOpeningDate = _targetOpeningDate,
                TargetStatus = _targetStatus
            };
        }
    }
}