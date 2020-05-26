using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class PublishedFundingVersionOrganisationGroupCsvBatchProcessorTests : BatchProcessorTestBase
    {
        protected Mock<IPublishedFundingOrganisationGroupingService> PublishedFundingOrganisationGroupingService;
        protected Mock<ISpecificationService> SpecificationService;
        protected Mock<IPublishedFundingVersionDataService> PublishedFundingVersionDataService;

        [TestInitialize]
        public void SetUp()
        {
            PublishedFundingOrganisationGroupingService = new Mock<IPublishedFundingOrganisationGroupingService>();
            SpecificationService = new Mock<ISpecificationService>();
            PublishedFundingVersionDataService = new Mock<IPublishedFundingVersionDataService>();

            BatchProcessor = new PublishedFundingVersionOrganisationGroupCsvBatchProcessor(PublishedFundingOrganisationGroupingService.Object,
                SpecificationService.Object,
                PublishedFundingVersionDataService.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                FileSystemAccess.Object,
                CsvUtils.Object);
        }
        
        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.History, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues, true)]

        public void SupportedJobTypes(FundingLineCsvGeneratorJobType jobType,
            bool expectedIsSupportedFlag)
        {
            BatchProcessor.IsForJobType(jobType)
                .Should()
                .Be(expectedIsSupportedFlag);
        }
        
        [TestMethod]
        public async Task ReturnsFalseIfNoResultsProcessed()
        { 
            string specificationId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodConfigId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingPeriodId(fundingPeriodConfigId));

            IEnumerable<PublishedFundingVersion> publishedFundingVersions = new List<PublishedFundingVersion>();
            IEnumerable<PublishedFundingOrganisationGrouping> publishedFundingOrganisationGroupings = new List<PublishedFundingOrganisationGrouping>();
            
            string expectedCsvOne = NewRandomString();

            GivenSpecificationSummaryRetrieved(specificationSummary);
            AndTheCurrentPublishedFundingVersionRetrieved(publishedFundingVersions);
            AndThePublishedFundingOrganisationGroupingGenerated(publishedFundingOrganisationGroupings);

            IEnumerable<PublishedFundingOrganisationGrouping> publishedFundingOrganisationGroupings1 = publishedFundingOrganisationGroupings
                    .OrderBy(_ => _.OrganisationGroupResult.GroupTypeCode.ToString());

            foreach (PublishedFundingOrganisationGrouping publishedFundingOrganisationGrouping in publishedFundingOrganisationGroupings1)
            {
                publishedFundingOrganisationGrouping.PublishedFundingVersions = publishedFundingOrganisationGrouping.PublishedFundingVersions.OrderByDescending(_ => _.Date);
            }

            GivenTheCsvRowTransformation(publishedFundingOrganisationGroupings1, 
                    new ExpandoObject[0], 
                    expectedCsvOne, 
                    true);

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues,
                specificationId,
                fundingPeriodId,
                NewRandomString(),
                fundingLineCode,
                fundingStreamId);

            processedResults
                .Should()
                .BeFalse();
        }
        
        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues)]
        public async Task TransformsPublishedProvidersForSpecificationInBatchesAndCreatesCsvWithResults(
            FundingLineCsvGeneratorJobType jobType)
        {
            string specificationId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodConfigId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            string expectedInterimFilePath = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingPeriodId(fundingPeriodConfigId));

            IEnumerable<PublishedFundingVersion> publishedFundingVersions = new List<PublishedFundingVersion>();

            IEnumerable<PublishedFundingOrganisationGrouping> publishedFundingOrganisationGroupings = new List<PublishedFundingOrganisationGrouping>
            {
                new PublishedFundingOrganisationGrouping
                {
                    OrganisationGroupResult = new OrganisationGroupResult
                    {
                        GroupTypeCode = OrganisationGroupTypeCode.LocalAuthority
                    },
                    PublishedFundingVersions = new List<PublishedFundingVersion>
                    {
                        new PublishedFundingVersion
                        {
                            Date = DateTime.Today
                        }
                    }
                },
                new PublishedFundingOrganisationGrouping
                {
                    OrganisationGroupResult = new OrganisationGroupResult
                    {
                        GroupTypeCode = OrganisationGroupTypeCode.Country
                    },
                    PublishedFundingVersions = new List<PublishedFundingVersion>
                    {
                        new PublishedFundingVersion
                        {
                            Date = DateTime.Today
                        }
                    }
                }
            };

            ExpandoObject[] transformedRowsOne = {
                new ExpandoObject(),
                new ExpandoObject(),
            };
            
            string expectedCsvOne = NewRandomString();

            GivenSpecificationSummaryRetrieved(specificationSummary);
            AndTheCurrentPublishedFundingVersionRetrieved(publishedFundingVersions);
            AndThePublishedFundingOrganisationGroupingGenerated(publishedFundingOrganisationGroupings);

            IEnumerable<PublishedFundingOrganisationGrouping> publishedFundingOrganisationGroupings1 = publishedFundingOrganisationGroupings
                    .OrderBy(_ => _.OrganisationGroupResult.GroupTypeCode.ToString());

            foreach (PublishedFundingOrganisationGrouping publishedFundingOrganisationGrouping in publishedFundingOrganisationGroupings1)
            {
                publishedFundingOrganisationGrouping.PublishedFundingVersions = publishedFundingOrganisationGrouping.PublishedFundingVersions.OrderByDescending(_ => _.Date);
            }

            GivenTheCsvRowTransformation(publishedFundingOrganisationGroupings1, 
                    transformedRowsOne, 
                    expectedCsvOne, 
                    true);

            bool processedResults = await WhenTheCsvIsGenerated(jobType, specificationId, fundingPeriodId, expectedInterimFilePath, fundingLineCode, fundingStreamId);

            processedResults
                .Should()
                .BeTrue();

            FileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath, 
                        expectedCsvOne, 
                        default),
                    Times.Once);
        }

        private void GivenSpecificationSummaryRetrieved(SpecificationSummary specificationSummary)
        {
            SpecificationService
                .Setup(_ => _.GetSpecificationSummaryById(It.IsAny<string>()))
                .Returns(Task.FromResult(specificationSummary));
        }

        private void AndTheCurrentPublishedFundingVersionRetrieved(IEnumerable<PublishedFundingVersion> publishedFundingVersions)
        {
            PublishedFundingVersionDataService
                .Setup(_ => _.GetPublishedFundingVersion(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(publishedFundingVersions));
        }

        private void AndThePublishedFundingOrganisationGroupingGenerated(IEnumerable<PublishedFundingOrganisationGrouping> organisationGroupings)
        {
            PublishedFundingOrganisationGroupingService
                .Setup(_ => _.GeneratePublishedFundingOrganisationGrouping(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<SpecificationSummary>(), It.IsAny<IEnumerable<PublishedFundingVersion>>()))
                .Returns(Task.FromResult(organisationGroupings));
        }
    }
}