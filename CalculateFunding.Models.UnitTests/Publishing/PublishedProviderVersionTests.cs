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
        public void AddCarryOversGuardsAgainstValuesEqualToZero(int overpayment)
        {
            Action invocation = () => WhenTheFundingLineCarryOverIsAdded(NewRandomString(), overpayment, ProfilingCarryOverType.DSGReProfiling);

            invocation
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Carry overs must not be equal to zero*");
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
        public void AddCarryOversGuardsAgainstAddingExistingFundingLine()
        {
            ProfilingCarryOver existingCarryOver = NewCarryOver(_ => _.WithFundingLineCode(NewRandomString()));
            ProfilingCarryOver newCarryOver = NewCarryOver(_ => _.WithFundingLineCode(existingCarryOver.FundingLineCode));
            GivenTheCarryOver(existingCarryOver);
            Action invocation = () => WhenTheFundingLineCarryOverIsAdded(existingCarryOver.FundingLineCode, NewRandomNumber(), NewRandomCarryOverType());

            invocation
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Carry over for funding line already exists");
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

        [TestMethod]
        public void AddOrUpdateCarryOversAddsToInternalCollectionIfDoesntExist()
        {
            string fundingLineCode = NewRandomString();
            decimal overPayment = NewRandomNumber();
            ProfilingCarryOverType type = NewRandomCarryOverType();
            WhenTheFundingLineCarryOverIsAddedOrUpdated(fundingLineCode, overPayment, type);

            _publishedProviderVersion.CarryOvers
                .Should()
                .BeEquivalentTo(new ProfilingCarryOver
                {
                    Type = type,
                    Amount = overPayment,
                    FundingLineCode = fundingLineCode
                });
        }

        [TestMethod]
        public void AddOrUpdateCarryOversUpdatesInternalCollectionIfFundlingLineExist()
        {
            ProfilingCarryOver existingCarryOver = NewCarryOver();
            GivenTheCarryOver(existingCarryOver);

            decimal newOverPayment = NewRandomNumber();
            ProfilingCarryOverType newType = NewRandomCarryOverType();
            WhenTheFundingLineCarryOverIsAddedOrUpdated(existingCarryOver.FundingLineCode, newOverPayment, newType);

            _publishedProviderVersion.CarryOvers
                .Should()
                .BeEquivalentTo(new ProfilingCarryOver
                {
                    Type = newType,
                    Amount = newOverPayment,
                    FundingLineCode = existingCarryOver.FundingLineCode
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

        [TestMethod]
        [DynamicData(nameof(SetIsIndicativeExamples), DynamicDataSourceType.Method)]
        public void SetsIndicativeFlagToTrueIsProviderStatusContainedInSuppliedSets(HashSet<string> indicativeStatus,
            string providerStatus,
            bool expectedIsIndicative)
        {
            GivenTheProviderStatus(providerStatus);
            
            WhenIsIndicativeIsSet(indicativeStatus);

            _publishedProviderVersion.IsIndicative
                .Should()
                .Be(expectedIsIndicative);
        }

        private void WhenIsIndicativeIsSet(HashSet<string> indicativeStatus)
            => _publishedProviderVersion.SetIsIndicative(indicativeStatus);
        
        private static IEnumerable<object[]> SetIsIndicativeExamples()
        {
            string statusOne = NewRandomString();
            string statusTwo = NewRandomString();

            yield return new object[]
            {
                AsHashset(statusOne, NewRandomString()),
                statusOne,
                true
            };
            yield return new object[]
            {
                AsHashset( NewRandomString(), statusTwo, NewRandomString()),
                statusTwo,
                true
            };
            yield return new object[]
            {
                AsHashset( NewRandomString(), NewRandomString()),
                NewRandomString(),
                false
            };
            yield return new object[]
            {
                AsHashset(),
                NewRandomString(),
                false
            };
        }

        private void GivenTheProviderStatus(string status)
            => _publishedProviderVersion.Provider = NewProvider(_ => _.Status = status);
        
        private static HashSet<string> AsHashset(params string[] items) => new HashSet<string>(items);

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

        private void WhenTheFundingLineCarryOverIsAddedOrUpdated(string fundingLineId, decimal overPayment, ProfilingCarryOverType type)
        {
            _publishedProviderVersion.AddOrUpdateCarryOver(fundingLineId, type, overPayment);
        }

        private void WhenTheFundingLineCarryOverIsRemoved(string fundingLineId)
        {
            _publishedProviderVersion.RemoveCarryOver(fundingLineId);
        }

        private static Provider NewProvider(Action<Provider> setUp = null)
        {
            Provider provider = new Provider();

            setUp?.Invoke(provider);
            
            return provider;
        }

        private static RandomEnum<ProfilingCarryOverType> NewRandomCarryOverType()
            => new RandomEnum<ProfilingCarryOverType>(ProfilingCarryOverType.Undefined);

        private static string NewRandomString() => new RandomString();

        private static decimal NewRandomNumber() => (decimal) new RandomNumberBetween(1, 999);
    }
}