using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Jobs
{
    [TestClass]
    public class JobDefinitionsServiceTests
    {
        private Mock<IJobDefinitionsRepository> _jobDefinitions;
        private Mock<ICacheProvider> _caching;
        private Mock<IValidator<JobDefinition>> _validator;

        private JobDefinitionsService _service;
        
        [TestInitialize]
        public void SetUp()
        {
            _jobDefinitions = new Mock<IJobDefinitionsRepository>();
            _caching = new Mock<ICacheProvider>();
            _validator = new Mock<IValidator<JobDefinition>>();
            
            _service = new JobDefinitionsService(_jobDefinitions.Object,
                Logger.None, 
                new ResiliencePolicies
                {
                    JobDefinitionsRepository = Policy.NoOpAsync(),
                    CacheProviderPolicy = Policy.NoOpAsync()
                }, 
                _caching.Object,
                _validator.Object);
        }

        [TestMethod]
        public void SaveDefinitionGuardsAgainstMissingDefinition()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheJobDefinitionIsSaved(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("definition");
        }

        [TestMethod]
        public void SaveDefinitionOnInvalidatesCacheOnSuccessfulSave()
        {
            JobDefinition jobDefinition = NewJobDefinition();
            
            GivenTheJobsRepositoryThrowsAnException(jobDefinition);

            Func<Task<IActionResult>> invocation = () => WhenTheJobDefinitionIsSaved(jobDefinition);

            invocation
                .Should()
                .Throw<Exception>();
            
            AndTheCacheWasNotInvalidated();
        }

        [TestMethod]
        public async Task SaveDefinitionDelegatesToRepositoryAndInvalidatesCache()
        {
            JobDefinition jobDefinition = NewJobDefinition();
            
            GivenTheJobsRepositoryReturnsTheStatusCode(jobDefinition, HttpStatusCode.OK);
            AndTheJobDefinitionValidationResult(jobDefinition, NewValidationResult());

            IActionResult actionResult = await WhenTheJobDefinitionIsSaved(jobDefinition);

            actionResult
                .Should()
                .BeOfType<NoContentResult>();
            
            ThenTheJobDefinitionWasSaved(jobDefinition);
            AndTheCacheWasInvalidated();
        }
        
        [TestMethod]
        public async Task SaveDefinitionExitsEarlyIfValidationFails()
        {
            JobDefinition jobDefinition = NewJobDefinition();

            string invalidProperty = NewRandomString();
            string validationFailureMessage = NewRandomString();
            
            GivenTheJobsRepositoryReturnsTheStatusCode(jobDefinition, HttpStatusCode.OK);
            GivenTheJobDefinitionValidationResult(jobDefinition, NewValidationResult(_ => 
                _.WithValidationFailures(NewValidationFailure(vf => 
                    vf.WithPropertyName(invalidProperty)
                        .WithErrorMessage(validationFailureMessage)))));

            BadRequestObjectResult actionResult = (await WhenTheJobDefinitionIsSaved(jobDefinition)) as BadRequestObjectResult;
            SerializableError serializableError = actionResult?.Value as SerializableError;

            serializableError
                .Should()
                .NotBeNull();

            serializableError[invalidProperty]
                .Should()
                .BeEquivalentTo(new [] {  validationFailureMessage });


            ThenNoJobDefinitionsWereSaved();
            AndTheCacheWasNotInvalidated();
        }

        [TestMethod]
        public async Task SaveDefinitionDelegatesToRepositoryAndReturnsStatusCodeIfNotSucceeded()
        {
            JobDefinition jobDefinition = NewJobDefinition();
            
            GivenTheJobsRepositoryReturnsTheStatusCode(jobDefinition, HttpStatusCode.Conflict);
            AndTheJobDefinitionValidationResult(jobDefinition, NewValidationResult());

            IActionResult actionResult = await WhenTheJobDefinitionIsSaved(jobDefinition);

            actionResult
                .Should()
                .BeOfType<StatusCodeResult>();
            
            ThenTheJobDefinitionWasSaved(jobDefinition);
            AndTheCacheWasNotInvalidated();
        }

        [TestMethod]
        public async Task GetJobDefinitionsDelegatesToRepositoryAndCachesResultsIfCacheEmpty()
        {
            JobDefinition[] jobDefinitions = new[]
            {
                NewJobDefinition(),
                NewJobDefinition(),
                NewJobDefinition()
            };
            
            GivenTheJobDefinitions(jobDefinitions);

            OkObjectResult result = (await WhenTheJobDefinitionsAreQueried()) as OkObjectResult;
            
            result?
                .Value
                .Should()
                .BeEquivalentTo(jobDefinitions);
            
            _caching.Verify(_ => _.SetAsync(CacheKeys.JobDefinitions, 
                    It.Is<List<JobDefinition>>(jds => jds.SequenceEqual(jobDefinitions)), 
                    null),
                Times.Once);
        }
        
        [TestMethod]
        public async Task GetJobDefinitionsReturnsCachedResultsIfCacheNotEmpty()
        {
            JobDefinition[] jobDefinitions = new[]
            {
                NewJobDefinition(),
                NewJobDefinition(),
                NewJobDefinition()
            };
            
            GivenTheCachedJobDefinitions(jobDefinitions);

            OkObjectResult result = (await WhenTheJobDefinitionsAreQueried()) as OkObjectResult;
            
            result?
                .Value
                .Should()
                .BeEquivalentTo(jobDefinitions);
            
            _jobDefinitions.Verify(_ => _.GetJobDefinitions(), Times.Never);
        }

        [TestMethod]
        public async Task GetByIdGetsAllAndReturnsFirstById()
        {
            string id = new RandomString();

            JobDefinition expectedJobDefinition = NewJobDefinition(_ => _.WithId(id));
            
            JobDefinition[] jobDefinitions = new[]
            {
                NewJobDefinition(),
                expectedJobDefinition,
                NewJobDefinition()
            };
            
            GivenTheCachedJobDefinitions(jobDefinitions);
            
            OkObjectResult result = (await WhenTheJobDefinitionIsQueried(id)) as OkObjectResult;
            
            result?
                .Value
                .Should()
                .BeSameAs(expectedJobDefinition);
        }

        [TestMethod]
        public async Task GetByIdGetsAllAndReturnsFirstByIdFromRepoIfNotCached()
        {
            string id = new RandomString();

            JobDefinition expectedJobDefinition = NewJobDefinition(_ => _.WithId(id));
            
            JobDefinition[] jobDefinitions = new[]
            {
                NewJobDefinition(),
                expectedJobDefinition,
                NewJobDefinition()
            };
            
            GivenTheJobDefinitions(jobDefinitions);
            
            OkObjectResult result = (await WhenTheJobDefinitionIsQueried(id)) as OkObjectResult;
            
            result?
                .Value
                .Should()
                .BeSameAs(expectedJobDefinition);
        }
        
        private async Task<IActionResult> WhenTheJobDefinitionIsQueried(string id)
        {
            return await _service.GetJobDefinitionById(id);
        }
        
        private void GivenTheJobDefinitions(params JobDefinition[] jobDefinitions)
        {
            _jobDefinitions.Setup(_ => _.GetJobDefinitions())
                .ReturnsAsync(jobDefinitions);
        }
        private void GivenTheCachedJobDefinitions(params JobDefinition[] jobDefinitions)
        {
            _caching.Setup(_ => _.GetAsync<List<JobDefinition>>(CacheKeys.JobDefinitions, null))
                .ReturnsAsync(jobDefinitions.ToList);
        }

        private async Task<IActionResult> WhenTheJobDefinitionsAreQueried()
        {
            return await _service.GetJobDefinitions();
        }

        private void AndTheCacheWasNotInvalidated()
        {
            _caching
                .Verify(_ => _.RemoveAsync<List<JobDefinition>>(CacheKeys.JobDefinitions),
                    Times.Never);
        }

        private void ThenTheJobDefinitionWasSaved(JobDefinition jobDefinition)
        {
            _jobDefinitions
                .Verify(_ => _.SaveJobDefinition(jobDefinition),
                    Times.Once);
        }
        
        private void ThenNoJobDefinitionsWereSaved()
        {
            _jobDefinitions
                .Verify(_ => _.SaveJobDefinition(It.IsAny<JobDefinition>()),
                    Times.Never);
        }

        private void AndTheCacheWasInvalidated()
        {
            _caching
                .Verify(_ => _.RemoveAsync<List<JobDefinition>>(CacheKeys.JobDefinitions),
                    Times.Once);
        }

        private void GivenTheJobsRepositoryThrowsAnException(JobDefinition jobDefinition)
        {
            _jobDefinitions.Setup(_ => _.SaveJobDefinition(jobDefinition))
                .ThrowsAsync(new Exception());
        }

        private void GivenTheJobsRepositoryReturnsTheStatusCode(JobDefinition jobDefinition, HttpStatusCode statusCode)
        {
            _jobDefinitions
                .Setup(_ => _.SaveJobDefinition(jobDefinition))
                .ReturnsAsync(statusCode);
        }

        private void GivenTheJobDefinitionValidationResult(JobDefinition jobDefinition, ValidationResult validationResult)
        {
            _validator.Setup(_ => _.ValidateAsync(jobDefinition, default))
                .ReturnsAsync(validationResult); 
        }
        
        private void AndTheJobDefinitionValidationResult(JobDefinition jobDefinition, ValidationResult validationResult)
        {
           GivenTheJobDefinitionValidationResult(jobDefinition, validationResult);
        }

        private ValidationResult NewValidationResult(Action<ValidationResultBuilder> setUp = null)
        {
            ValidationResultBuilder validationResultBuilder = new ValidationResultBuilder();
            
            setUp?.Invoke(validationResultBuilder);
            
            return validationResultBuilder
                .Build();
        }

        private ValidationFailure NewValidationFailure(Action<ValidationFailureBuilder> setUp = null)
        {
            ValidationFailureBuilder validationFailureBuilder = new ValidationFailureBuilder();

            setUp?.Invoke(validationFailureBuilder);
            
            return validationFailureBuilder.Build();
        }
        
        private JobDefinition NewJobDefinition(Action<JobDefinitionBuilder> setUp = null)
        {
            JobDefinitionBuilder jobDefinition = new JobDefinitionBuilder();
            
            setUp?.Invoke(jobDefinition);
            
            return jobDefinition.Build();
        }

        private async Task<IActionResult> WhenTheJobDefinitionIsSaved(JobDefinition jobDefinition)
        {
            return await _service.SaveDefinition(jobDefinition);
        }

        private string NewRandomString() => new RandomString();
    }
}
