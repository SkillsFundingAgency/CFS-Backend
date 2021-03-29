using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Reporting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class GenerateApprovePublishedFundingCsvJobsCreationTests : GeneratePublishedFundingCsvJobsCreationTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            JobsCreation = new GenerateApprovePublishedFundingCsvJobsCreation(
                CreateGeneratePublishedFundingCsvJobs.Object, CreateGeneratePublishedProviderEstateCsvJobs.Object);     
        }
        
        [TestMethod]
        public async Task CreatesProviderStateCsvJobForEachJobType()
        {
            string specificationId = NewRandomString();
            IEnumerable<(string Code, string Name)> fundingLines = new[] { 
                (NewRandomString(), NewRandomString()), 
                (NewRandomString(), NewRandomString())
            };
            IEnumerable<string> fundingStreamIds = Array.Empty<string>();
            string correlationId = NewRandomString();
            Reference user = NewUser();

            await WhenTheJobsAreCreated(specificationId, correlationId, user, fundingLines, fundingStreamIds);

            ThenNoProviderEstateCsvJobsWereCreated(specificationId, correlationId, user);
        }
    }
}