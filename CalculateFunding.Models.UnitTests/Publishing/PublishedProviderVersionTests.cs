using System;
using System.Collections.Generic;
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
                .ThrowExactly<InvalidOperationException>()
                .WithMessage($"Unsupported {nameof(ProfilingCarryOverType)}");
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void AddCarryOversGuardsAgainstValuesLessThanOrEqualToZero(int overpayment)
        {
            Action invocation = () => WhenTheFundingLineCarryOverIsAdded(NewRandomString(), overpayment, ProfilingCarryOverType.DSGReProfiling);

            invocation
                .Should()
                .ThrowExactly<InvalidOperationException>()
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
                .ThrowExactly<ArgumentNullException>()
                .WithMessage("Funding Line Id cannot be missing*");
        }

        [TestMethod]
        public void AddCarryOversAddsToInternalCollection()
        {
            string fundingLineId = NewRandomString();
            decimal overPayment = NewRandomNumber();
            ProfilingCarryOverType type = NewRandomCarryOverType();
            
            WhenTheFundingLineCarryOverIsAdded(fundingLineId, overPayment, type);

            _publishedProviderVersion.CarryOvers
                .Should()
                .BeEquivalentTo(new ProfilingCarryOver
                {
                    Type = type,
                    Amount = overPayment,
                    FundingLineCode = fundingLineId
                });
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void RemoveCarryOversGuardsAgainstMissingFundingLineIds(string fundingLineId)
        {
            Action invocation = () => WhenTheFundingLineCarryOverIsRemoved(fundingLineId);

            invocation
                .Should()
                .ThrowExactly<ArgumentNullException>()
                .WithMessage("Funding Line Id cannot be missing*");
        }

        [TestMethod]
        public void RemoveCarryOversRemovesFromInternalCollectionWhereFundingLineCodeMatches()
        {
            string fundingLineCode = NewRandomString();

            ProfilingCarryOver customCarryOver = NewCarryOver(_ => _.WithFundingLineCode(fundingLineCode));
            ProfilingCarryOver differentFundingLineCarryOver = NewCarryOver();

            GivenTheCarryOver(customCarryOver);
            AndTheCarryOver(differentFundingLineCarryOver);

            WhenTheFundingLineCarryOverIsRemoved(fundingLineCode);

            _publishedProviderVersion
                .CarryOvers
                .Should()
                .BeEquivalentTo(differentFundingLineCarryOver);
        }

        private ProfilingCarryOver NewCarryOver(Action<ProfileCarryOverBuilder> setUp = null)
        {
            ProfileCarryOverBuilder profileCarryOverBuilder = new ProfileCarryOverBuilder();

            setUp?.Invoke(profileCarryOverBuilder);
            
            return profileCarryOverBuilder.Build();
        }

        private void GivenTheCarryOver(ProfilingCarryOver carryOver)
        {
            _publishedProviderVersion.CarryOvers ??= new List<ProfilingCarryOver>();
            _publishedProviderVersion.CarryOvers.Add(carryOver);
        }

        private void AndTheCarryOver(ProfilingCarryOver carryOver)
            => GivenTheCarryOver(carryOver);

        private void WhenTheFundingLineCarryOverIsAdded(string fundingLineId, decimal overPayment, ProfilingCarryOverType type)
        {
            _publishedProviderVersion.AddCarryOver(fundingLineId, type, overPayment);
        }

        private void WhenTheFundingLineCarryOverIsRemoved(string fundingLineId)
        {
            _publishedProviderVersion.RemoveCarryOver(fundingLineId);
        }

        private static RandomEnum<ProfilingCarryOverType> NewRandomCarryOverType()
            => new RandomEnum<ProfilingCarryOverType>(ProfilingCarryOverType.Undefined);

        private string NewRandomString() => new RandomString();

        private decimal NewRandomNumber() => (decimal) new RandomNumberBetween(1, 999);
    }
}