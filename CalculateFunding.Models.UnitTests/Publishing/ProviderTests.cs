using System;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Models.UnitTests.Publishing
{
    [TestClass]
    public class ProviderTests
    {
        [TestMethod]
        public void GetSuccessorsReturnsEmptyArrayIfNeitherCollectionOrSinglePropertyPopulated()
        {
            NewProvider()
                .GetSuccessors()
                .Should()
                .BeEquivalentTo(ArraySegment<string>.Empty);
        }
        
        [TestMethod]
        public void GetSuccessorsReturnsCollectionIfPopulated()
        {
            string[] expectedSuccessors = new[]
            {
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString()
            };
            
            NewProvider(_ => _.Successors = expectedSuccessors)
                .GetSuccessors()
                .Should()
                .BeEquivalentTo(expectedSuccessors);
        }
        
        [TestMethod]
        public void GetSuccessorsReturnsCollectionIfBothPopulated()
        {
            string[] expectedSuccessors = new[]
            {
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString()
            };
            
            NewProvider(_ =>
                {
                    _.Successors = expectedSuccessors;
                    _.Successor = NewRandomString();
                })
                .GetSuccessors()
                .Should()
                .BeEquivalentTo(expectedSuccessors);
        }
        
        [TestMethod]
        public void GetSuccessorsReturnsSinglePropertyIfCollectionNotPopulated()
        {
            string successor = NewRandomString();
            NewProvider(_ => _.Successor = successor)
                .GetSuccessors()
                .Should()
                .BeEquivalentTo(successor);
        }

        public Provider NewProvider(Action<Provider> setUp = null)
        {
            Provider provider = new Provider();

            setUp?.Invoke(provider);
            
            return provider;
        }

        private string NewRandomString() => new RandomString();

    }
}