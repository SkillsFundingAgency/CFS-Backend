using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ProfilingCarryOverBuilder : TestEntityBuilder
    {
        private string _fundingLineCode;
        private ProfilingCarryOverType? _type;
        private decimal? _amount;

        public ProfilingCarryOverBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public ProfilingCarryOverBuilder WithType(ProfilingCarryOverType type)
        {
            _type = type;

            return this;
        }

        public ProfilingCarryOverBuilder WithAmount(decimal amount)
        {
            _amount = amount;

            return this;
        }
        
        
        public ProfilingCarryOver Build()
        {
            return new ProfilingCarryOver
            {
                Type = _type.GetValueOrDefault(NewRandomEnum(ProfilingCarryOverType.Undefined)),
                Amount = _amount.GetValueOrDefault(NewRandomNumberBetween(1, int.MaxValue)),
                FundingLineCode = _fundingLineCode ?? NewRandomString()
            };
        }
    }
}