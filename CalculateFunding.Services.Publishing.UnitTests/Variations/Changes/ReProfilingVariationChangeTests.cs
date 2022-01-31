using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class ReProfilingVariationChangeTests : ReProfilingVariationChangeTestsBase
    {
        protected override string Strategy => "ReProfiling";
        protected override string ChangeName => "Re-profiling variation change";

        [TestInitialize]
        public void SetUp()
        {
            Change = new ReProfileVariationChange(VariationContext, Strategy);
        }
    }
}