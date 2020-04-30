using System.Collections.Generic;
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
using ExpandoObject = System.Dynamic.ExpandoObject;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class PublishedFundingOrganisationGroupCsvBatchProcessorTests : BatchProcessorTestBase
    {
        private Mock<IPublishedFundingOrganisationGroupingService> _publishedFundingOrganisationGroupingService;
        private Mock<ISpecificationService> _specificationService;
        private Mock<IPublishedFundingDataService> _publishedFundingDataService;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFundingOrganisationGroupingService = new Mock<IPublishedFundingOrganisationGroupingService>();
            _specificationService = new Mock<ISpecificationService>();
            _publishedFundingDataService = new Mock<IPublishedFundingDataService>();

            BatchProcessor = new PublishedFundingOrganisationGroupCsvBatchProcessor(_publishedFundingOrganisationGroupingService.Object,
                _specificationService.Object,
                _publishedFundingDataService.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                FileSystemAccess.Object,
                CsvUtils.Object);
        }

        [TestMethod]
        public async Task ReturnsFalseIfNoResultsProcessed()
        {
            string specificationId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingPeriodId(fundingPeriodId));
 
            IEnumerable<PublishedFunding> publishedFunding = new List<PublishedFunding>
            {
                new PublishedFunding
                {
                    Current = new PublishedFundingVersion()
                }
            };

            IEnumerable<PublishedFundingOrganisationGrouping> publishedFundingOrganisationGroupings = new List<PublishedFundingOrganisationGrouping>
            {
                new PublishedFundingOrganisationGrouping
                {
                    OrganisationGroupResult = new OrganisationGroupResult
                    {
                        GroupTypeCode = OrganisationGroupTypeCode.LocalAuthority
                    }
                }
            };

            ExpandoObject[] transformedRowsOne = new ExpandoObject[0];

            string expectedCsvOne = NewRandomString();

            GivenSpecificationSummaryRetrieved(specificationSummary);
            AndTheCurrentPublishedFundingRetrieved(publishedFunding);
            AndThePublishedFundingOrganisationGroupingGenerated(publishedFundingOrganisationGroupings);
            AndTheCsvRowTransformation(
                publishedFundingOrganisationGroupings
                    .OrderBy(_ => _.OrganisationGroupResult.GroupTypeCode.ToString()),
                transformedRowsOne,
                expectedCsvOne,
                true);

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues,
                specificationId,
                NewRandomString(),
                fundingLineCode,
                fundingStreamId);

            processedResults
                .Should()
                .BeFalse();
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.History, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues, true)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues, false)]

        public void SupportedJobTypes(FundingLineCsvGeneratorJobType jobType,
            bool expectedIsSupportedFlag)
        {
            BatchProcessor.IsForJobType(jobType)
                .Should()
                .Be(expectedIsSupportedFlag);
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues)]
        public async Task TransformsPublishedProvidersForSpecificationInBatchesAndCreatesCsvWithResults(
            FundingLineCsvGeneratorJobType jobType)
        {
            string specificationId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            string expectedInterimFilePath = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingPeriodId(fundingPeriodId));

            IEnumerable<PublishedFunding> publishedFunding = new List<PublishedFunding>
            {
                new PublishedFunding
                {
                    Current = new PublishedFundingVersion()
                }
            };

            IEnumerable<PublishedFundingOrganisationGrouping> publishedFundingOrganisationGroupings = new List<PublishedFundingOrganisationGrouping>
            {
                new PublishedFundingOrganisationGrouping
                {
                    OrganisationGroupResult = new OrganisationGroupResult
                    {
                        GroupTypeCode = OrganisationGroupTypeCode.LocalAuthority
                    }
                },
                new PublishedFundingOrganisationGrouping
                {
                    OrganisationGroupResult = new OrganisationGroupResult
                    {
                        GroupTypeCode = OrganisationGroupTypeCode.Country
                    }
                }
            };

            ExpandoObject[] transformedRowsOne = {
                new ExpandoObject(),
                new ExpandoObject(),
            };
            
            string expectedCsvOne = NewRandomString();

            GivenSpecificationSummaryRetrieved(specificationSummary);
            AndTheCurrentPublishedFundingRetrieved(publishedFunding);
            AndThePublishedFundingOrganisationGroupingGenerated(publishedFundingOrganisationGroupings);
            AndTheCsvRowTransformation(
                publishedFundingOrganisationGroupings
                    .OrderBy(_ => _.OrganisationGroupResult.GroupTypeCode.ToString()), 
                    transformedRowsOne, 
                    expectedCsvOne, 
                    true);

            bool processedResults = await WhenTheCsvIsGenerated(jobType, specificationId, expectedInterimFilePath, fundingLineCode, fundingStreamId);

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
            _specificationService
                .Setup(_ => _.GetSpecificationSummaryById(It.IsAny<string>()))
                .Returns(Task.FromResult(specificationSummary));
        }

        private void AndTheCurrentPublishedFundingRetrieved(IEnumerable<PublishedFunding> publishedFunding)
        {
            _publishedFundingDataService
                .Setup(_ => _.GetCurrentPublishedFunding(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(publishedFunding));
        }

        private void AndThePublishedFundingOrganisationGroupingGenerated(IEnumerable<PublishedFundingOrganisationGrouping> organisationGroupings)
        {
            _publishedFundingOrganisationGroupingService
                .Setup(_ => _.GeneratePublishedFundingOrganisationGrouping(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<SpecificationSummary>(), It.IsAny<IEnumerable<PublishedFundingVersion>>()))
                .Returns(Task.FromResult(organisationGroupings));
        }
    }
}