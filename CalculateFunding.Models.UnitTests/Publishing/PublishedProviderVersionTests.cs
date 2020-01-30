using System;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Models.UnitTests.Publishing
{
    [TestClass]
    public class PublishedProviderVersionTests
    {
        private PublishedProviderVersion _publishedProviderVersion;

        [TestInitialize]
        public void SetUp()
        {
            _publishedProviderVersion = new PublishedProviderVersion();
        }
        
        
        [DataTestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void AddFundingLineOverPaymentGuardsAgainstValuesLessThanOrEqualToZero(int overpayment)
        {
            Action invocation = () => WhenTheFundingLineOverPaymentIsAdded(NewRandomString(), overpayment);

            invocation
                .Should()
                .ThrowExactly<ArgumentOutOfRangeException>()
                .WithMessage("Over payments must be greater than zero*");
        }
        
        [DataTestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void AddFundingLineOverPaymentGuardsAgainstMissingFundingLineIds(string fundingLineId)
        {
            Action invocation = () => WhenTheFundingLineOverPaymentIsAdded(fundingLineId, NewRandomNumber());

            invocation
                .Should()
                .ThrowExactly<ArgumentOutOfRangeException>()
                .WithMessage("Funding Line Id cannot be missing*");
        }

        [TestMethod]
        public void AddFundingLineOverPaymentAddsToInternalCollection()
        {
            string fundingLineId = NewRandomString();
            decimal overPayment = NewRandomNumber();
            
            WhenTheFundingLineOverPaymentIsAdded(fundingLineId, overPayment);

            _publishedProviderVersion
                .FundingLineOverPayments
                .Should()
                .NotBeNull();

            _publishedProviderVersion
                .FundingLineOverPayments.TryGetValue(fundingLineId, out decimal actualOverpayment)
                .Should()
                .BeTrue();

            actualOverpayment
                .Should()
                .Be(overPayment);
        }
        
        private void WhenTheFundingLineOverPaymentIsAdded(string fundingLineId, decimal overPayment)
        {
            _publishedProviderVersion.AddFundingLineOverPayment(fundingLineId, overPayment);
        }
        
        private string NewRandomString() => new RandomString();

        private decimal NewRandomNumber() => (decimal) new RandomNumberBetween(1, 999);
    }
}