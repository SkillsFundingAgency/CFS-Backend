using System;
using System.Collections.Generic;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Comparers;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Comparers
{
    [TestClass]
    public class ProviderComparerTests
    {
        private ProviderComparer _providerComparer;
        
        [TestInitialize]
        public void SetUp()
        {
            _providerComparer = new ProviderComparer();
        }
        
        [TestMethod]
        [DynamicData(nameof(SuccessorComparisonExamples), DynamicDataSourceType.Method)]
        public void ComparesSuccessorsIndependentOfOrderAndCase(Provider a,
            Provider b,
            bool equals)
        {
            _providerComparer.Equals(a, b)
                .Should()
                .Be(equals);
        }

        private static IEnumerable<object[]> SuccessorComparisonExamples()
        {
            string successorOne = NewRandomString();
            string successorTwo = NewRandomString();
            string successorThree = NewRandomString();
            string successorFour = NewRandomString();

            Provider providerOne = NewProvider();
            Provider providerTwo = providerOne.DeepCopy();
            Provider providerThree = providerOne.DeepCopy();
            Provider providerFour = providerOne.DeepCopy();
            Provider providerFive = providerOne.DeepCopy();
            Provider providerSix = providerOne.DeepCopy();
            Provider providerSeven = providerOne.DeepCopy();
            Provider providerEight = providerOne.DeepCopy();

            providerOne.Successors = AsArray(successorFour, successorTwo, successorOne, successorThree);
            providerTwo.Successors = AsArray( successorOne.ToLower(), successorTwo, successorFour.ToUpper(), successorThree);

            providerThree.Successors = AsArray(successorFour);
            providerFour.Successors = null;

            providerFive.Successors = null;
            providerSix.Successors = null;

            providerSeven.Successors = AsArray(NewRandomString(), NewRandomString());
            providerEight.Successors = AsArray(NewRandomString(), NewRandomString());
            
            yield return new object[]
            {
                providerOne,
                providerTwo,
                true
            };
            yield return new object[]
            {
                providerThree,
                providerFour,
                false
            };
            yield return new object[]
            {
                providerFive,
                providerSix,
                true
            };
            yield return new object[]
            {
                providerEight,
                providerSeven,
                false
            };
        }

        private static Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);
            
            return providerBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();

        private static string[] AsArray(params string[] values) => values;
    }
}