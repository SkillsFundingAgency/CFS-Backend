using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests.Services
{
    public class ProfilePatternReProfilingConfigurationBuilder : TestEntityBuilder
    {
        private bool? _enabled;
        private string _increase;
        private string _decrease;
        private string _same;
        private string _initialFunding;

        public ProfilePatternReProfilingConfigurationBuilder WithIsEnabled(bool isEnabled)
        {
            _enabled = isEnabled;

            return this;
        }
        
        public ProfilePatternReProfilingConfigurationBuilder WithInitialFundingStrategyKey(string initialFunding)
        {
            _initialFunding = initialFunding;

            return this;
        }

        public ProfilePatternReProfilingConfigurationBuilder WithDecreasedAmountStrategyKey(string decreasedAmountStrategyKey)
        {
            _decrease = decreasedAmountStrategyKey;

            return this;
        }
        
        public ProfilePatternReProfilingConfigurationBuilder WithIncreasedAmountStrategyKey(string increasedAmountStrategyKey)
        {
            _increase = increasedAmountStrategyKey;

            return this;
        }
        
        public ProfilePatternReProfilingConfigurationBuilder WithSameAmountStrategyKey(string sameAmountStrategyKey)
        {
            _same = sameAmountStrategyKey;

            return this;
        }
        
        public ProfilePatternReProfilingConfiguration Build()
        {
            return new ProfilePatternReProfilingConfiguration
            {
                ReProfilingEnabled   =  _enabled.GetValueOrDefault(NewRandomFlag()),
                SameAmountStrategyKey = _same,
                DecreasedAmountStrategyKey = _decrease,
                IncreasedAmountStrategyKey = _increase,
                InitialFundingStrategyKey = _initialFunding
            };
        }
    }
}