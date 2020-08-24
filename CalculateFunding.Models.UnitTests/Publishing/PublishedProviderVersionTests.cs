using System;
using System.Linq;
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
        
        [TestMethod]
        public void AddCarryOversGuardsAgainstUndefinedType()
        {
            Action invocation = () => WhenTheFundingLineCarryOverIsAdded(NewRandomString(), NewRandomNumber(), ProfilingCarryOverType.Undefined);

            invocation
                .Should()
                .ThrowExactly<ArgumentOutOfRangeException>()
                .WithMessage($"Unsupported {nameof(ProfilingCarryOverType)} (Parameter 'type')");
        }
        
        
        [DataTestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void AddCarryOversGuardsAgainstValuesLessThanOrEqualToZero(int overpayment)
        {
            Action invocation = () => WhenTheFundingLineCarryOverIsAdded(NewRandomString(), overpayment, ProfilingCarryOverType.DSGReProfiling);

            invocation
                .Should()
                .ThrowExactly<ArgumentOutOfRangeException>()
                .WithMessage("Carry overs must be greater than zero*");
        }
        
        [DataTestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void AddCarryOversGuardsAgainstMissingFundingLineIds(string fundingLineId)
        {
            Action invocation = () => WhenTheFundingLineCarryOverIsAdded(fundingLineId, NewRandomNumber(), ProfilingCarryOverType.DSGReProfiling);

            invocation
                .Should()
                .ThrowExactly<ArgumentOutOfRangeException>()
                .WithMessage("Funding Line Id cannot be missing*");
        }

        [TestMethod]
        public void AddCarryOversAddsToInternalCollection()
        {
            string fundingLineId = NewRandomString();
            decimal overPayment = NewRandomNumber();
            ProfilingCarryOverType type = new RandomEnum<ProfilingCarryOverType>(ProfilingCarryOverType.Undefined);
            
            WhenTheFundingLineCarryOverIsAdded(fundingLineId, overPayment, type);

            _publishedProviderVersion
                .CarryOvers
                .Should()
                .NotBeNull();

            ProfilingCarryOver carryOver = _publishedProviderVersion.CarryOvers.FirstOrDefault(_ => _.FundingLineCode == fundingLineId);

            carryOver
                .Should()
                .BeEquivalentTo(new ProfilingCarryOver
                {
                    Type = type,
                    Amount = overPayment,
                    FundingLineCode = fundingLineId
                });
        }
        
        private void WhenTheFundingLineCarryOverIsAdded(string fundingLineId, decimal overPayment, ProfilingCarryOverType type)
        {
            _publishedProviderVersion.AddCarryOver(fundingLineId, type, overPayment);
        }
        
        private string NewRandomString() => new RandomString();

        private decimal NewRandomNumber() => (decimal) new RandomNumberBetween(1, 999);
    }
}