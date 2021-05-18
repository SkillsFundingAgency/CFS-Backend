using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.UnitTests.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{
    [TestClass]
    public class PublishingDatasetsDataCopyJobCreationTests : JobCreationForSpecificationTestBase<PublishingDatasetsDataCopyJobCreation>
    {
        [TestInitialize]
        public void SetUp()
        {
            JobCreation = new PublishingDatasetsDataCopyJobCreation(Jobs,
                Logger);
        }
    }
}