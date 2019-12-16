using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class PublishProviderFundingJobCreationTests : JobCreationForSpecificationTestBase<PublishProviderFundingJobDefinition>
    {
        public PublishProviderFundingJobCreationTests()
        {
            JobDefinition = new PublishProviderFundingJobDefinition();
        }
    }
}