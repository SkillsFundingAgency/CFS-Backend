using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Castle.Core.Resource;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderUpdateDateServiceTests
    {
        private Mock<IPublishedFundingRepository> _publishedFundingRepository;

        private PublishedProviderUpdateDateService _service;
        
        [TestInitialize]
        public void SetUp()
        {
            _publishedFundingRepository = new Mock<IPublishedFundingRepository>();
            
            _service = new PublishedProviderUpdateDateService(_publishedFundingRepository.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public async Task DelegatesToRepositoryAndReturnsQueryResult()
        {
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            
            DateTime? expectedResult = new RandomDateTime();

            _publishedFundingRepository.Setup(_ => _.GetLatestPublishedDate(fundingStreamId, fundingPeriodId))
                .ReturnsAsync(expectedResult);
            
            OkObjectResult result = await _service.GetLatestPublishedDate(fundingStreamId, fundingPeriodId) as OkObjectResult;

            result?.Value
                .Should()
                .Be(expectedResult);
        }
        
        private static string NewRandomString() => new RandomString();
    }
}