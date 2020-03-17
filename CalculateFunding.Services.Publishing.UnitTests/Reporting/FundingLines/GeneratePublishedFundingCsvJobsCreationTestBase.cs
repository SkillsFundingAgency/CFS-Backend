using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Interfaces;
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
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public void GuardsAgainstMissingUser()
        {
            Func<Task> invocation = () => WhenTheJobsAreCreated(NewRandomString(),
                NewRandomString(),
                null,
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("user");
        }

        [TestMethod]
        public void GuardsAgainstMissingFundingLineCodes()
        {
            Func<Task> invocation = () => WhenTheJobsAreCreated(NewRandomString(),
                NewRandomString(),
                NewUser(),
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("fundingLineCodes");
        }

        [TestMethod]
        public async Task CreatesCsvJobForEachJobType()
        {
            string specificationId = NewRandomString();
            IEnumerable<string> fundingLineCodes = new[] { NewRandomString(), NewRandomString() };
            string correlationId = NewRandomString();
            Reference user = NewUser();

            await WhenTheJobsAreCreated(specificationId, correlationId, user, fundingLineCodes);

            ThenTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.History, null);
            AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.Released, null);
            AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentState, null);

            foreach (string fundingLineCode in fundingLineCodes)
            {
                AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentProfileValues, fundingLineCode);
                AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.HistoryProfileValues, fundingLineCode);
            }
        }

        protected void ThenTheProviderEstateCsvJobsWasCreated(string specificationId,
            string correlationId,
            Reference user)
        {
            CreateGeneratePublishedProviderEstateCsvJobs.Verify(_ => _.CreateJob(specificationId, user, correlationId,
                    It.IsAny<Dictionary<string, string>>(),
                    null),
                Times.Once);
        }
        
        protected void ThenNoProviderEstateCsvJobsWereCreated(string specificationId,
            string correlationId,
            Reference user)
        {
            CreateGeneratePublishedProviderEstateCsvJobs.Verify(_ => _.CreateJob(specificationId, user, correlationId,
                    It.IsAny<Dictionary<string, string>>(),
                    null),
                Times.Never);
        }

        private void ThenTheJobWasCreated(string specificationId,
            string correlationId,
            Reference user,
            FundingLineCsvGeneratorJobType jobType,
            string fundingLineCode)
        {
            CreateGeneratePublishedFundingCsvJobs.Verify(_ => _.CreateJob(specificationId, user, correlationId,
                    It.Is<Dictionary<string, string>>(props
                        => props.ContainsKey(JobType) &&
                           props[JobType] == jobType.ToString() &&
                           props.ContainsKey(FundingLineCode) &&
                           props[FundingLineCode] == fundingLineCode),
                    null),
                Times.Once);
        }

        protected void AndTheJobWasCreated(string specificationId,
            string correlationId,
            Reference user,
            FundingLineCsvGeneratorJobType jobType,
            string fundingLineCode)
        {
            ThenTheJobWasCreated(specificationId, correlationId, user, jobType, fundingLineCode);
        }

        protected Task WhenTheJobsAreCreated(string specificationId, string correlationId, Reference user, IEnumerable<string> fundingLineCodes)
        {
            return JobsCreation.CreateJobs(specificationId, correlationId, user, fundingLineCodes);
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