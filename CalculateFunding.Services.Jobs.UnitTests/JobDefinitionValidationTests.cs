using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Services.Jobs.Validators;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Jobs
{
    [TestClass]
    public class JobDefinitionValidationTests
    {
        private Mock<IJobDefinitionsRepository> _jobDefinitions;
        private JobDefinitionValidator _validator;

        [TestInitialize]
        public void SetUp()
        {
            _jobDefinitions = new Mock<IJobDefinitionsRepository>();

            _validator = new JobDefinitionValidator(_jobDefinitions.Object,
                new ResiliencePolicies
                {
                    JobDefinitionsRepository = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public async Task FailsValidationIfIdMissing()
        {
            ValidationResult validationResult = await WhenTheJobDefinitionIsValidated(NewJobDefinition(_ =>
                _.WithQueueName(NewRandomString())
                    .WithTopicName(NewRandomString())
                    .WithoutId()));

            ThenTheValidationResultsAre(validationResult,
                ("Id", "You must supply a job definition id"));
        }

        private void ThenTheValidationResultsAre(ValidationResult validationResult,
            params (string, string)[] expectedResults)
        {
            validationResult.Errors.Count
                .Should()
                .Be(expectedResults?.Length ?? 0);

            foreach (var expectedResult in expectedResults)
            {
                validationResult
                    .Errors
                    .Count(_ => _.PropertyName == expectedResult.Item1 &&
                                _.ErrorMessage == expectedResult.Item2)
                    .Should()
                    .Be(1);
            }
        }

        private async Task<ValidationResult> WhenTheJobDefinitionIsValidated(JobDefinition jobDefinition)
        {
            return await _validator.ValidateAsync(jobDefinition);
        }

        private JobDefinition NewJobDefinition(Action<JobDefinitionBuilder> setUp = null)
        {
            JobDefinitionBuilder jobDefinitionBuilder = new JobDefinitionBuilder();

            setUp?.Invoke(jobDefinitionBuilder);

            return jobDefinitionBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}