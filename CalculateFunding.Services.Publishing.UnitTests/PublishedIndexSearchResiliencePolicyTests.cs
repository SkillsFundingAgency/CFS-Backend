using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Helper;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.Rest.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedIndexSearchResiliencePolicyTests
    {     

        [TestMethod]
        public void PublishedIndexSearchPolicy_GivenInvalidSearchException_ReturnsExceptionAsync()
        {
            //Arrange           
            IPublishingResiliencePolicies resiliencePolicies = GenerateTestPolicies();

            //Act             
            Func<Task> test = async () =>  await resiliencePolicies.PublishedIndexSearchResiliencePolicy.ExecuteAsync(async () => await CreateInValidSearchCloudException());

            //Assert
            test.Should().Equals(true);          
        }

        [TestMethod]
        public void PublishedIndexSearchPolicy_GivenValidSearchException_SupressException()
        {
            //Arrange           
            IPublishingResiliencePolicies resiliencePolicies = GenerateTestPolicies();

            //Act
            Func<Task> test = async () => await resiliencePolicies.PublishedIndexSearchResiliencePolicy.ExecuteAsync(async () => CreateValidSearchCloudException());

            //Assert          
            test
                .Should()
                .NotThrow<CloudException>();               
        }

        private async Task CreateInValidSearchCloudException()
        {
            await Task.Delay(1);
            throw new CloudException("Some Other error");
        }

        private async Task CreateValidSearchCloudException()
        {
            await Task.Delay(1);
            throw new CloudException("Another indexer invocation is currently in progress; concurrent invocations not allowed.");   
        }

        public static IPublishingResiliencePolicies GenerateTestPolicies()
        {

            AsyncPolicy circuitBreakerRequestException = Policy.Handle<Exception>().CircuitBreakerAsync(100, TimeSpan.FromMinutes(1));

            return new ResiliencePolicies()
            {
                FundingFeedSearchRepository = Policy.NoOpAsync(),
                PublishedFundingBlobRepository = Policy.NoOpAsync(),
                PublishedIndexSearchResiliencePolicy = PublishedIndexSearchResiliencePolicy.GeneratePublishedIndexSearch()
            };
        }
    }
}
