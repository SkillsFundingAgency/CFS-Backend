using CalculateFunding.Tests.Common;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IReleaseManagementIdentifierGeneratorStepContext
    {
        SequentialGuidIdentifierGenerator FundingGroup { get; set; }
        SequentialGuidIdentifierGenerator FundingGroupVersion { get; set; }
        SequentialGuidIdentifierGenerator FundingGroupVersionVariationReasons { get; set; }
        SequentialGuidIdentifierGenerator ProviderVariationReasons { get; set; }
        SequentialGuidIdentifierGenerator ReleasedProvider { get; set; }
        SequentialGuidIdentifierGenerator ReleasedProviderVersion { get; set; }
        SequentialGuidIdentifierGenerator FundingGroupProvider { get; set; }
        SequentialGuidIdentifierGenerator ReleasedProviderVersionChannel { get; set; }
    }
}
