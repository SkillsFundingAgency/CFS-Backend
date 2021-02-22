using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Analysis.ObsoleteItems;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis.ObsoleteItems
{
    [TestClass]
    public class FundingLineReferenceCleanUpTests : ObsoleteItemCleanUpTest
    {
        [TestInitialize]
        public void SetUp()
        {
            ObsoleteItemType = ObsoleteItemType.FundingLine;
            
            CleanUp = new FundingLineReferenceCleanUp(Calculations.Object,
                new ResiliencePolicies
                {
                    CalculationsRepository = Policy.NoOpAsync(),
                    CalculationsRepositoryNoOCCRetry = Policy.NoOpAsync()
                });
        }     
    }
}