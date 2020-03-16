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
    [TestClass]
    public class GeneratePublishedFundingCsvJobsCreationTests
    {
        private const string JobType = "job-type";
        private const string FundingLineCode = "funding-line-code";

        private Mock<ICreateGeneratePublishedFundingCsvJobs> _createGeneratePublishedFundingCsvJobs;
        private Mock<ICreateGeneratePublishedProviderEstateCsvJobs> _createGeneratePublishedProviderEstateCsvJobs;

        private GenerateApprovePublishedFundingCsvJobsCreation _jobsCreation;

        [TestInitialize]
        public void SetUp()
        {
            _createGeneratePublishedFundingCsvJobs = new Mock<ICreateGeneratePublishedFundingCsvJobs>();
            _createGeneratePublishedProviderEstateCsvJobs = new Mock<ICreateGeneratePublishedProviderEstateCsvJobs>();

            _jobsCreation = new GenerateApprovePublishedFundingCsvJobsCreation(
                _createGeneratePublishedFundingCsvJobs.Object, _createGeneratePublishedProviderEstateCsvJobs.Object);
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
            }
        }

        private void ThenTheJobWasCreated(string specificationId,
            string correlationId,
            Reference user,
            FundingLineCsvGeneratorJobType jobType,
            string fundingLineCode)
        {
            _createGeneratePublishedFundingCsvJobs.Verify(_ => _.CreateJob(specificationId, user, correlationId,
                    It.Is<Dictionary<string, string>>(props
                        => props.ContainsKey(JobType) &&
                           props[JobType] == jobType.ToString() &&
                           props.ContainsKey(FundingLineCode) &&
                           props[FundingLineCode] == fundingLineCode),
                    null),
                Times.Once);
        }

        private void AndTheJobWasCreated(string specificationId,
            string correlationId,
            Reference user,
            FundingLineCsvGeneratorJobType jobType,
            string fundingLineCode)
        {
            ThenTheJobWasCreated(specificationId, correlationId, user, jobType, fundingLineCode);
        }

        private Task WhenTheJobsAreCreated(string specificationId, string correlationId, Reference user, IEnumerable<string> fundingLineCodes)
        {
            return _jobsCreation.CreateJobs(specificationId, correlationId, user, fundingLineCodes);
        }

        private Reference NewUser()
        {
            return new ReferenceBuilder()
                .Build();
        }

        private string NewRandomString()
        {
            return new RandomString();
        }
    }
}