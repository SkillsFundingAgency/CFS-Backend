using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    public abstract class GeneratePublishedFundingCsvJobsCreationTestBase
    {
        private const string JobType = "job-type";
        private const string FundingLineCode = "funding-line-code";
        private const string FundingLineName = "funding-line-name";
        private const string FundingStreamId = "funding-stream-id";

        protected Mock<ICreateGeneratePublishedFundingCsvJobs> CreateGeneratePublishedFundingCsvJobs;
        protected Mock<ICreateGeneratePublishedProviderEstateCsvJobs> CreateGeneratePublishedProviderEstateCsvJobs;
        protected BaseGeneratePublishedFundingCsvJobsCreation JobsCreation;

        [TestInitialize]
        public void GeneratePublishedFundingCsvJobsCreationTestBaseSetUp()
        {
            CreateGeneratePublishedFundingCsvJobs = new Mock<ICreateGeneratePublishedFundingCsvJobs>();
            CreateGeneratePublishedProviderEstateCsvJobs = new Mock<ICreateGeneratePublishedProviderEstateCsvJobs>();
        }

        [TestMethod]
        public void GuardsAgainstMissingSpecificationId()
        {
            Func<Task> invocation = () => WhenTheJobsAreCreated(null,
                NewRandomString(),
                NewUser(),
                null,
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("SpecificationId");
        }

        [TestMethod]
        public void GuardsAgainstMissingUser()
        {
            Func<Task> invocation = () => WhenTheJobsAreCreated(NewRandomString(),
                NewRandomString(),
                null,
                null,
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("User");
        }

        [TestMethod]
        public void GuardsAgainstMissingFundingLineCodes()
        {
            Func<Task> invocation = () => WhenTheJobsAreCreated(NewRandomString(),
                NewRandomString(),
                NewUser(),
                null,
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("FundingLines");
        }

        [TestMethod]
        public void GuardsAgainstMissingFundingStreamIds()
        {
            Func<Task> invocation = () => WhenTheJobsAreCreated(NewRandomString(),
                NewRandomString(),
                NewUser(),
                new List<(string Code, string Name)>(),
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("FundingStreamIds");
        }

        [TestMethod]
        public async Task CreatesCsvJobForEachJobType()
        {
            string specificationId = NewRandomString();
            IEnumerable<(string Code, string Name)> fundingLines = new[] { 
                (NewRandomString(), NewRandomString()),
                (NewRandomString(), NewRandomString()) 
            };
            IEnumerable<string> fundingStreamIds = new[] { NewRandomString(), NewRandomString() };
            string correlationId = NewRandomString();
            Reference user = NewUser();

            await WhenTheJobsAreCreated(specificationId, correlationId, user, fundingLines, fundingStreamIds);

            ThenTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.History, null, null, null);
            AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.Released, null, null, null);
            AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentState, null, null, null);

            foreach ((string Code, string Name) in fundingLines)
            {
                AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentProfileValues, Code, Name, fundingStreamIds.FirstOrDefault());
                AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.HistoryProfileValues, Code, Name, fundingStreamIds.FirstOrDefault());
            }
        }

        protected void ThenTheProviderEstateCsvJobsWasCreated(string specificationId,
            string correlationId,
            Reference user)
        {
            CreateGeneratePublishedProviderEstateCsvJobs.Verify(_ => _.CreateJob(specificationId, user, correlationId,
                    It.IsAny<Dictionary<string, string>>(),
                    null,
                    null,
                    false),
                Times.Once);
        }
        
        protected void ThenNoProviderEstateCsvJobsWereCreated(string specificationId,
            string correlationId,
            Reference user)
        {
            CreateGeneratePublishedProviderEstateCsvJobs.Verify(_ => _.CreateJob(specificationId, user, correlationId,
                    It.IsAny<Dictionary<string, string>>(),
                    null,
                    null,
                    false),
                Times.Never);
        }

        private void ThenTheJobWasCreated(string specificationId,
            string correlationId,
            Reference user,
            FundingLineCsvGeneratorJobType jobType,
            string fundingLineCode,
            string fundingLineName,
            string fundingStreamId)
        {
            CreateGeneratePublishedFundingCsvJobs.Verify(_ => _.CreateJob(specificationId, user, correlationId,
                    It.Is<Dictionary<string, string>>(props
                        => props.ContainsKey(JobType) &&
                           props[JobType] == jobType.ToString() &&
                           props.ContainsKey(FundingLineCode) &&
                           props[FundingLineCode] == fundingLineCode &&
                           props[FundingLineName] == fundingLineName &&
                           props.ContainsKey(FundingStreamId) &&
                           props[FundingStreamId] == fundingStreamId),
                    null,
                    null,
                    false),
                Times.Once);
        }

        protected void AndTheJobWasCreated(string specificationId,
            string correlationId,
            Reference user,
            FundingLineCsvGeneratorJobType jobType,
            string fundingLineCode,
            string fundingLineName,
            string fundingStreamId)
        {
            ThenTheJobWasCreated(specificationId, correlationId, user, jobType, fundingLineCode, fundingLineName, fundingStreamId);
        }

        protected Task WhenTheJobsAreCreated(string specificationId, string correlationId, Reference user, IEnumerable<(string Code, string Name)> fundingLines, IEnumerable<string> fundingStreamIds)
        {
            PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest = new PublishedFundingCsvJobsRequest
            {
                SpecificationId = specificationId,
                CorrelationId = correlationId,
                User = user,
                FundingLines = fundingLines,
                FundingStreamIds = fundingStreamIds
            };
            return JobsCreation.CreateJobs(publishedFundingCsvJobsRequest);
        }

        protected Reference NewUser()
        {
            return new ReferenceBuilder()
                .Build();
        }

        protected string NewRandomString()
        {
            return new RandomString();
        }
    }
}