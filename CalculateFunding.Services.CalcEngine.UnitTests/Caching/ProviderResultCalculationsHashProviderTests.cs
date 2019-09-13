using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.CalcEngine.Caching;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Calculator.Caching
{
    [TestClass]
    public class ProviderResultCalculationsHashProviderTests
    {
        private ICacheProvider _cacheProvider;
        private string _specificationId;
        private string _providerId;
        private int _partitionSize;
        private int _partitionIndex;

        private ProviderResultCalculationsHashProvider _calculationsHashProvider;

        [TestInitialize]
        public void SetUp()
        {
            _partitionIndex = NewRandomNumber();
            _partitionSize = NewRandomNumber();
            _specificationId = NewRandomString();
            _providerId = NewRandomString();

            _cacheProvider = Substitute.For<ICacheProvider>();

            _calculationsHashProvider = new ProviderResultCalculationsHashProvider(_cacheProvider);

            _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
                .Returns(Task.CompletedTask);
        }

        [TestMethod]
        public void TryUpdate_ThrowsExceptionIfBatchNotStartedForSpecificationId()
        {
            Func<bool> invocation = () => WhenTheProviderResultCacheIsUpdated(NewProviderResult(_ => _.WithCalculationResults(NewCalculationResult(),
                NewCalculationResult())));

            invocation
                .Should()
                .Throw<InvalidOperationException>();
        }
        
        [TestMethod]
        public void EndBatch_ThrowsExceptionIfBatchNotStartedForSpecificationId()
        {
            Action invocation = AndTheBatchIsEnded; 

            invocation
                .Should()
                .Throw<InvalidOperationException>();
        }
        
        [TestMethod]
        public void EndBatch_StopsTheCurrentBatch()
        {  
            WhenTheBatchIsStarted();
            
            AndTheBatchIsEnded();
            
            Action invocation = AndTheBatchIsEnded; 

            invocation
                .Should()
                .Throw<InvalidOperationException>();
        }
        
        [TestMethod]
        public void StartBatch_ThrowsExceptionIfBatchAlreadyStartedForSpecificationId()
        {
            WhenTheBatchIsStarted();
            
            Action invocation = WhenTheBatchIsStarted; 

            invocation
                .Should()
                .Throw<InvalidOperationException>();
        }
        
        [TestMethod]
        public void CreatesNewDictionaryIfNoneCachedForProviderIdAndSpecificationIdYet()
        {
            ProviderResult providerResult = NewProviderResult(_ => _.WithCalculationResults(NewCalculationResult(),
                NewCalculationResult()));
            string expectedCachedHash = GetComputedHash(providerResult.CalculationResults);
            
            WhenTheBatchIsStarted();
            
            bool wasUpdated = AndTheProviderResultCacheIsUpdated(providerResult);
            
            AndTheBatchIsEnded();

            wasUpdated
                .Should()
                .BeTrue();

            AndTheResultsHashDictionaryWasStored(new Dictionary<string, string>
            {
                {providerResult.Provider.Id, expectedCachedHash}
            });
        }

        [TestMethod]
        public void UpdatesCacheEntryIfCachedHashDiffersForProviderIdAndSpecificationId()
        {
            ProviderResult providerResult = NewProviderResult(_ => _.WithCalculationResults(NewCalculationResult(),
                NewCalculationResult()));
            string expectedCachedHash = GetComputedHash(providerResult.CalculationResults);
            
            GivenTheExistingResultsHashesForSpecification(_providerId, "a different hash", "a second provider", "a different hash");
            
            WhenTheBatchIsStarted();
            
            bool wasUpdated = AndTheProviderResultCacheIsUpdated(providerResult);
            
            AndTheBatchIsEnded();

            wasUpdated
                .Should()
                .BeTrue();

            AndTheResultsHashDictionaryWasStored(new Dictionary<string, string>
            {
                {providerResult.Provider.Id, expectedCachedHash},
                {"a second provider", "a different hash"}
            });
        }
        
        [TestMethod]
        public void ReturnsFalseIfCachedHashUnchangedForProviderIdAndSpecificationId()
        {
            ProviderResult providerResult = NewProviderResult(_ => _.WithCalculationResults(NewCalculationResult(),
                NewCalculationResult()));
            string expectedCachedHash = GetComputedHash(providerResult.CalculationResults);
            
            GivenTheExistingResultsHashesForSpecification(_providerId, expectedCachedHash, "a second provider", "a different hash");

            WhenTheBatchIsStarted();
            
            bool wasUpdated = AndTheProviderResultCacheIsUpdated(providerResult);
            
            AndTheBatchIsEnded();

            wasUpdated
                .Should()
                .BeFalse();

            AndTheResultsHashDictionaryWasStored(new Dictionary<string, string>
            {
                {providerResult.Provider.Id, expectedCachedHash},
                {"a second provider", "a different hash"}
            });
        }
        
        private bool WhenTheProviderResultCacheIsUpdated(ProviderResult providerResult)
        {
            bool isUpdated = _calculationsHashProvider.TryUpdateCalculationResultHash(providerResult,
                _partitionIndex,
                _partitionSize);
            
            return isUpdated;
        }

        private bool AndTheProviderResultCacheIsUpdated(ProviderResult providerResult)
        {
            return WhenTheProviderResultCacheIsUpdated(providerResult);
        }

        private void WhenTheBatchIsStarted()
        {
            _calculationsHashProvider.StartBatch(_specificationId, _partitionIndex, _partitionSize);    
        }
        
        private void AndTheBatchIsEnded()
        {
            _calculationsHashProvider.EndBatch(_specificationId, _partitionIndex, _partitionSize);   
        }
        
        private string GetComputedHash(IEnumerable<CalculationResult> calculationResults)
        {
            return calculationResults
                .AsJson()
                .ComputeSHA1Hash();
        }

        private CalculationResult NewCalculationResult(Action<CalculationResultBuilder> setUp = null)
        {
            CalculationResultBuilder builder = new CalculationResultBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private ProviderResult NewProviderResult(Action<ProviderResultBuilder> setUp = null)
        {
            ProviderResultBuilder builder = new ProviderResultBuilder()
                .WithSpecificationId(_specificationId)
                .WithProviderId(_providerId);

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private void GivenTheExistingResultsHashesForSpecification(params string[] keyValuePairs)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            for (int parameter = 0; parameter < keyValuePairs.Length; parameter += 2)
            {
                results[keyValuePairs[parameter]] = keyValuePairs[parameter + 1];
            }

            _cacheProvider.GetAsync<Dictionary<string, string>>(
                    CacheKeyForSpecification())
                .Returns(results);
        }

        private void AndTheResultsHashDictionaryWasStored(Dictionary<string, string> expectedDictionary)
        {
            _cacheProvider.SetAsync(CacheKeyForSpecification(),
                Arg.Is<Dictionary<string, string>>(actualDictionary => DictionariesMatch(expectedDictionary, actualDictionary)));
        }

        private bool DictionariesMatch(Dictionary<string, string> expected, Dictionary<string, string> actual)
        {
            return expected.Keys.SequenceEqual(actual.Keys) &&
                   expected.Keys.All(_ => expected[_] == actual[_]);
        }

        private int NewRandomNumber()
        {
            return new RandomNumberBetween(1, 3000);
        }

        private string NewRandomString()
        {
            return new RandomString();
        }

        private string CacheKeyForSpecification()
        {
            return $"{CacheKeys.CalculationResults}{_specificationId}:{_partitionIndex}-{_partitionSize}";
        }
    }
}