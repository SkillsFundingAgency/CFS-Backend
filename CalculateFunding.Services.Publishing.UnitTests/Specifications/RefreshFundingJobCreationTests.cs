using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class RefreshFundingJobCreationTests : JobCreationForSpecificationTestBase<RefreshFundingJobDefinition>
    {
        public RefreshFundingJobCreationTests()
        {
            JobDefinition = new RefreshFundingJobDefinition();
        }
    }
}