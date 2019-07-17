using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class RefreshFundingJobCreationTests : JobCreationForSpecificationTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            JobDefinitionId = JobConstants.DefinitionNames.RefreshFundingJob;
            TriggerMessage = "Requesting publication of specification";

            JobCreation = new RefreshFundingJobCreation(Jobs,
                ResiliencePolicies,
                Logger);
        }
    }
}