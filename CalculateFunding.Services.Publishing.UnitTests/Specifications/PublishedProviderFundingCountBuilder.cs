using System;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    public class PublishedProviderFundingCountBuilder : TestEntityBuilder
    {
        public PublishedProviderFundingCount Build()
        {
            return new PublishedProviderFundingCount
            {
                Count = NewRandomNumberBetween(1, 100),
                TotalFunding = NewRandomNumberBetween(10000, Int32.MaxValue)
            };
        }
    }
}