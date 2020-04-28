using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class PublishProviderFundingJobCreationTests : JobCreationForSpecificationTestBase<AllPublishProviderFundingJobCreation>
    {
        [TestInitialize]
        public void SetUp()
        {
            JobCreation = new AllPublishProviderFundingJobCreation(Jobs,
                ResiliencePolicies,
                Logger);
        }
    }
}