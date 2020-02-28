using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.UnitTests.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{
    [TestClass]
    public class GeneratePublishedFundingCsvJobCreationTests : JobCreationForSpecificationTestBase<GeneratePublishedFundingCsvJobCreation>
    {
        [TestInitialize]
        public void SetUp()
        {
            JobCreation = new GeneratePublishedFundingCsvJobCreation(Jobs,
                ResiliencePolicies,
                Logger);
        }
    }
}