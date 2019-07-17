using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class PublishProviderFundingJobCreationTests : JobCreationForSpecificationTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            JobDefinitionId = JobConstants.DefinitionNames.PublishProviderFundingJob;
            TriggerMessage = "Requesting publication of provider funding";

            JobCreation = new PublishProviderFundingJobCreation(Jobs,
                ResiliencePolicies,
                Logger);
        }
    }
}