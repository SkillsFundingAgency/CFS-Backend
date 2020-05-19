using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class RefreshFundingJobCreationTests : JobCreationForSpecificationTestBase<RefreshFundingJobCreation>
    {
        [TestInitialize]
        public void SetUp()
        {
            JobCreation = new RefreshFundingJobCreation(Jobs,
                Logger);
        }
    }
}