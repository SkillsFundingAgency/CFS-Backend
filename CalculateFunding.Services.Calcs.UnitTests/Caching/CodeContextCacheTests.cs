using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Code;
using CalculateFunding.Services.Calcs.Caching;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Services;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Calcs.UnitTests.Caching
{
    [TestClass]
    public class CodeContextCacheTests
    {
        private const string SpecificationIdKey = "specification-id";
        private const string JobIdKey = "jobId";
        
        private Mock<IJobManagement> _jobs;
        private Mock<ICodeContextBuilder> _codeContextBuilder;
        private Mock<ICacheProvider> _cache;

        private CodeContextCache _codeContextCache;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobManagement>();
            _codeContextBuilder = new Mock<ICodeContextBuilder>();
            _cache = new Mock<ICacheProvider>();
            
            _codeContextCache = new CodeContextCache(_cache.Object,
                _codeContextBuilder.Object,
                _jobs.Object,
                new ResiliencePolicies
                {
                    CacheProviderPolicy = Policy.NoOpAsync(),
                }, 
                Logger.None);
        }

        [TestMethod]
        public void QueueCodeContextCacheUpdateGuardsAgainstMissingSpecificationId()
        {
            Func<Task<IActionResult>> invocation = () => WhenACodeContextUpdateJobIsQueued(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public async Task QueueCodeContextCacheUpdateCreatesNewUpdateCodeContextJobForSuppliedSpecificationId()
        {
            string specificationId = NewRandomString();

            await WhenACodeContextUpdateJobIsQueued(specificationId);
            
            ThenANewUpdateCodeContextJobWasQueued(specificationId);
        }

        [TestMethod]
        public void UpdateCodeContextCacheEntryGuardsAgainstMissingServiceBusMessage()
        {
            Func<Task> invocation = () => WhenACodeContextCacheEntryIsUpdated(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("message");
        }

        [TestMethod]
        public void UpdateCodeContextCacheEntryGuardsAgainstMissingSpecificationIdInMessageProperties()
        {
            Message message = NewMessage(_ => _.WithUserProperty(JobIdKey, NewRandomString()));

            Func<Task> invocation = () => WhenACodeContextCacheEntryIsUpdated(message);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be(SpecificationIdKey);
        }
        
        [TestMethod]
        public void UpdateCodeContextCacheEntryGuardsAgainstMissingJobIdInMessageProperties()
        {
            Message message = NewMessage(_ => _.WithUserProperty(SpecificationIdKey, NewRandomString()));

            Func<Task> invocation = () => WhenACodeContextCacheEntryIsUpdated(message);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be(JobIdKey);
        }

        [TestMethod]
        public async Task UpdateCodeContextCacheEntryBuildsContextForSuppliedSpecificationIdAndUpdatesCachedEntryWithResult()
        {
            string jobId = NewRandomString();
            string specificationId = NewRandomString();

            Message message = NewMessage(_ => _.WithUserProperty(SpecificationIdKey, specificationId)
                .WithUserProperty(JobIdKey, jobId));

            TypeInformation[] codeContext = new[]
            {
                NewTypeInformation(), NewTypeInformation(), NewTypeInformation()
            };
            
            GivenTheNewCodeContextForSpecificationId(specificationId, codeContext);
            AndTheJobCanBeRun(jobId);

            await WhenACodeContextCacheEntryIsUpdated(message);
            
            ThenTheJobWasStarted(jobId);
            AndTheCodeContextWasCached(GetCacheKey(specificationId), codeContext);
            AndTheJobWasCompleted(jobId);
        }
        
        [TestMethod]
        public async Task GetCodeContextLazilyInitialisesCacheEntryIfNotPreviouslyCachedForTheSuppliedSpecificationId()
        {
            string specificationId = NewRandomString();

            TypeInformation[] expectedCodeContext = new[]
            {
                NewTypeInformation(), NewTypeInformation(), NewTypeInformation()
            };
            
            GivenTheNewCodeContextForSpecificationId(specificationId, expectedCodeContext);

            IEnumerable<TypeInformation> actualCodeContext =  await WhenTheCodeContextIsQueried(specificationId);

            actualCodeContext
                .Should()
                .BeEquivalentTo<TypeInformation>(expectedCodeContext);
            
            AndTheCodeContextWasCached(GetCacheKey(specificationId), expectedCodeContext);
        }
        
        [TestMethod]
        public async Task GetCodeContextReturnsTheCurrentlyCachedEntryIfPreviouslyCachedForTheSuppliedSpecificationId()
        {
            string specificationId = NewRandomString();

            TypeInformation[] expectedCodeContext = new[]
            {
                NewTypeInformation(), NewTypeInformation(), NewTypeInformation()
            };
            
            GivenTheCodeContextCacheEntry(specificationId, expectedCodeContext);

            IEnumerable<TypeInformation> actualCodeContext =  await WhenTheCodeContextIsQueried(specificationId);

            actualCodeContext
                .Should()
                .BeEquivalentTo<TypeInformation>(expectedCodeContext);
        }

        private async Task<IEnumerable<TypeInformation>> WhenTheCodeContextIsQueried(string specificationId)
            => await _codeContextCache.GetCodeContext(specificationId);

        private static string GetCacheKey(string specificationId) => $"{CacheKeys.CodeContext}{specificationId}";
        
        private void AndTheCodeContextWasCached(string cacheKey,
            TypeInformation[] codeContext)
        {
            _cache.Verify(_ => _.SetAsync(cacheKey, It.Is<TypeInformation[]>(ti =>
                ti.SequenceEqual(codeContext)), null),
                Times.Once);
        }

        private void AndTheJobCanBeRun(string jobId)
        {
            _jobs.Setup(_ => _.RetrieveJobAndCheckCanBeProcessed(jobId))
                .ReturnsAsync(new JobViewModel { Id = jobId });
        }

        private void ThenTheJobWasStarted(string jobId)
        {
            VerifyJobLogWasAdded(jobId, null);
        }

        private void AndTheJobWasCompleted(string jobId)
        {
            VerifyJobLogWasAdded(jobId, true);    
        }

        private void VerifyJobLogWasAdded(string jobId,
            bool? completedSuccessfully)
            => _jobs.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, completedSuccessfully, null),
                Times.Once);

        private void GivenTheNewCodeContextForSpecificationId(string specificationId,
            IEnumerable<TypeInformation> codeContext)
            => _codeContextBuilder.Setup(_ => _.BuildCodeContextForSpecification(specificationId))
                .ReturnsAsync(codeContext);
        
        private void GivenTheCodeContextCacheEntry(string specificationId,
            IEnumerable<TypeInformation> codeContext)
        {
            string cacheKey = GetCacheKey(specificationId);

            _cache.Setup(_ => _.KeyExists<TypeInformation[]>(cacheKey))
                .ReturnsAsync(true);
            _cache.Setup(_ => _.GetAsync<TypeInformation[]>(cacheKey, null))
                .ReturnsAsync(codeContext.ToArray());
        }

        private Message NewMessage(Action<MessageBuilder> setUp = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder();

            setUp?.Invoke(messageBuilder);
            
            return messageBuilder.Build();
        } 
        
        private TypeInformation NewTypeInformation() => new TypeInformation();

        private async Task WhenACodeContextCacheEntryIsUpdated(Message message)
            => await _codeContextCache.Run(message);

        private void ThenANewUpdateCodeContextJobWasQueued(string specificationId)
        {
            _jobs.Verify(_ => _.QueueJob(It.Is<JobCreateModel>(job => 
                job.SpecificationId == specificationId &&
                job.JobDefinitionId == JobConstants.DefinitionNames.UpdateCodeContextJob &&
                job.Properties != null &&
                job.Properties.ContainsKey(SpecificationIdKey) &&
                job.Properties[SpecificationIdKey] == specificationId)),
                Times.Once);
        }
        
        private async Task<IActionResult> WhenACodeContextUpdateJobIsQueued(string specificationId)
            => await _codeContextCache.QueueCodeContextCacheUpdate(specificationId);
        
        private string NewRandomString() => new RandomString();
    }
}