using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class GenerateReleasePublishedFundingCsvJobsCreationTests : GeneratePublishedFundingCsvJobsCreationTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            JobsCreation = new GenerateReleasePublishedFundingCsvJobsCreation(
                CreateGeneratePublishedFundingCsvJobs.Object, CreateGeneratePublishedProviderEstateCsvJobs.Object);     
        }
        
        [TestMethod]
        public async Task CreatesProviderStateCsvJobForEachJobType()
        {
            string specificationId = NewRandomString();
            IEnumerable<string> fundingLineCodes = new[] { NewRandomString(), NewRandomString() };
            IEnumerable<string> fundingStreamIds = new[] { NewRandomString(), NewRandomString() };
            string correlationId = NewRandomString();
            Reference user = NewUser();

            await WhenTheJobsAreCreated(specificationId, correlationId, user, fundingLineCodes, fundingStreamIds);

            ThenNoProviderEstateCsvJobsWereCreated(specificationId, correlationId, user);
        }

        [TestMethod]
        public async Task CreatesPublishedOrganisationGroupCsvJobs()
        {
            string specificationId = NewRandomString();
            IEnumerable<string> fundingLineCodes = new[] { NewRandomString(), NewRandomString() };
            IEnumerable<string> fundingStreamIds = new[] { NewRandomString(), NewRandomString() };
            string correlationId = NewRandomString();
            Reference user = NewUser();

            await WhenTheJobsAreCreated(specificationId, correlationId, user, fundingLineCodes, fundingStreamIds);

            foreach (string fundingStreamId in fundingStreamIds)
            {
                AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues, null, fundingStreamId);
                AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues, null, fundingStreamId);
            }
        }
    }
}