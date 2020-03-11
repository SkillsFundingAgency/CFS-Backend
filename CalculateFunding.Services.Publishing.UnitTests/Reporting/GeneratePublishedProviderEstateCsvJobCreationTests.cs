using CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate;
using CalculateFunding.Services.Publishing.UnitTests.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{
    [TestClass]
    public class GeneratePublishedProviderEstateCsvJobCreationTests 
        : JobCreationForSpecificationTestBase<CreateGeneratePublishedProviderEstateCsvJobs>
    {
        [TestInitialize]
        public void SetUp()
        {
            JobCreation = new CreateGeneratePublishedProviderEstateCsvJobs(Jobs,
                ResiliencePolicies,
                Logger);
        }
    }
}