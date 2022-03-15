using CalculateFunding.Tests.Common;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class ReleaseManagementIdentifierGeneratorStepContext : IReleaseManagementIdentifierGeneratorStepContext
    {
        public ReleaseManagementIdentifierGeneratorStepContext()
        {
            FundingGroup = new SequentialGuidIdentifierGenerator();
            FundingGroupVersion = new SequentialGuidIdentifierGenerator();
            FundingGroupVersionVariationReasons = new SequentialGuidIdentifierGenerator();
            ProviderVariationReasons = new SequentialGuidIdentifierGenerator();
            ReleasedProvider = new SequentialGuidIdentifierGenerator();
            ReleasedProviderVersion = new SequentialGuidIdentifierGenerator();
            FundingGroupProvider = new SequentialGuidIdentifierGenerator();
            ReleasedProviderVersionChannel = new SequentialGuidIdentifierGenerator();
        }

        public SequentialGuidIdentifierGenerator FundingGroup { get; set; }

        public SequentialGuidIdentifierGenerator FundingGroupVersion { get; set; }

        public SequentialGuidIdentifierGenerator FundingGroupVersionVariationReasons { get; set; }

        public SequentialGuidIdentifierGenerator ProviderVariationReasons { get; set; }
        public SequentialGuidIdentifierGenerator ReleasedProvider { get; set; }
        public SequentialGuidIdentifierGenerator ReleasedProviderVersion { get; set; }
        public SequentialGuidIdentifierGenerator FundingGroupProvider { get; set; }
        public SequentialGuidIdentifierGenerator ReleasedProviderVersionChannel { get; set; }
    }
}
