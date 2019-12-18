using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Providers
{
    [TestClass]
    public class DeletePublishedProvidersServiceTests
    {
        private ICreateDeletePublishedProvidersJobs _deletePublishedProviders;
        private DeletePublishedProvidersService _service;
        
        [TestInitialize]
        public void SetUp()
        {
            _deletePublishedProviders = Substitute.For<ICreateDeletePublishedProvidersJobs>();
            
            _service = new DeletePublishedProvidersService(_deletePublishedProviders);
        }

        [TestMethod]
        [DynamicData(nameof(ExceptionExamples), DynamicDataSourceType.Method)]
        public void OnlyCorrelationIdIsOptional(string fundingStreamId,
            string fundingPeriodId,
            Reference user)
        {
            Func<Task> invocation = () => WhenADeletePublishedProvidersJobIsQueued(fundingStreamId,
                fundingPeriodId,
                user,
                null);

            invocation
                .Should()
                .ThrowAsync<Exception>();
        }

        [TestMethod]
        public async Task QueueDeleteJobDelegatesToDeleteJobCreationService()
        {
            string correlationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            Reference user = NewUser();

            await WhenADeletePublishedProvidersJobIsQueued(fundingStreamId, fundingPeriodId, user, correlationId);

            await _deletePublishedProviders
                .Received(1)
                .CreateJob(fundingStreamId,
                    fundingPeriodId,
                    user,
                    correlationId);
        }

        public static IEnumerable<object[]> ExceptionExamples()
        {
            yield return new object[] {null, NewRandomString(), NewUser()};
            yield return new object[] {NewRandomString(), null, NewUser()};
            yield return new object[] {NewRandomString(), NewRandomString(), null};
        }

        private Task WhenADeletePublishedProvidersJobIsQueued(string fundingStreamId, 
            string fundingPeriodId, 
            Reference user, 
            string correlationId)
        {
            return _service.QueueDeletePublishedProvidersJob(fundingStreamId,  fundingPeriodId, user, correlationId);
        }

        private static Reference NewUser(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);
            
            return referenceBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();
    }
}