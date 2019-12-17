using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class DeleteSpecificationJobCreationTests : JobCreationForSpecificationTestBase<DeleteSpecificationJobCreation>
    {
        [TestInitialize]
        public void SetUp()
        {
            JobCreation = new DeleteSpecificationJobCreation(Jobs,
                ResiliencePolicies,
                Logger);
        }
    }
}