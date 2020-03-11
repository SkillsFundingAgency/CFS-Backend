using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{
    [TestClass]
    public class GeneratePublishedFundingCsvJobsCreationTests
    {
        private const string JobType = "job-type";

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
                NewUser());

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
        public async Task CreatesCsvJobForEachJobType()
        {
            string specificationId = NewRandomString();
            string correlationId = NewRandomString();
            Reference user = NewUser();

            await WhenTheJobsAreCreated(specificationId, correlationId, user);

            ThenTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.History);
            AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.Released);
            AndTheJobWasCreated(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentState);
        }

        private void ThenTheJobWasCreated(string specificationId,
            string correlationId,
            Reference user,
            FundingLineCsvGeneratorJobType jobType)
        {
            _createGeneratePublishedFundingCsvJobs.Verify(_ => _.CreateJob(specificationId, user, correlationId,
                    It.Is<Dictionary<string, string>>(props
                        => props.ContainsKey(JobType) &&
                           props[JobType] == jobType.ToString()),
                    null),
                Times.Once);
        }

        private void AndTheJobWasCreated(string specificationId,
            string correlationId,
            Reference user,
            FundingLineCsvGeneratorJobType jobType)
        {
            ThenTheJobWasCreated(specificationId, correlationId, user, jobType);
        }

        private Task WhenTheJobsAreCreated(string specificationId, string correlationId, Reference user)
        {
            return _jobsCreation.CreateJobs(specificationId, correlationId, user);
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