using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using TechTalk.SpecFlow;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class ReleaseManagementIdentifiersStepDefinitions
    {
        private readonly IReleaseManagementIdentifierGeneratorStepContext _context;

        public ReleaseManagementIdentifiersStepDefinitions(IReleaseManagementIdentifierGeneratorStepContext context)
        {
            _context = context;
        }

        [Given(@"the next funding group provider identifier for new records should be (.*)")]
        public void GivenTheNextFundingGroupProviderIdentifierShouldBe(int nextId)
        {
            _context.FundingGroupProvider.NextId = nextId;
        }

        [Given(@"the next released provider version identifier for new records should be (.*)")]
        public void GivenTheNextReleasedProviderVersionIdentifierForNewRecordsShouldBe(int nextId)
        {
            _context.ReleasedProviderVersion.NextId = nextId;
        }

        [Given(@"the next released provider version channel identifier for new records should be (.*)")]
        public void GivenTheNextReleasedProviderVersionChannelIdentifierForNewRecordsShouldBe(int nextId)
        {
            _context.ReleasedProviderVersionChannel.NextId = nextId;
        }

        [Given(@"the next funding group version identifier for new records should be (.*)")]
        public void GivenTheNextFundingGroupVersionIdentifierForNewRecordsShouldBe(int nextId)
        {
            _context.FundingGroupVersion.NextId = nextId;
        }


    }
}
