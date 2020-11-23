using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Models.UnitTests.Publishing
{
    public class ProfileCarryOverBuilder : TestEntityBuilder
    {
        private ProfilingCarryOverType? _type;
        private decimal? _carryOver;
        private string _fundingLineCode;

        public ProfileCarryOverBuilder WithType(ProfilingCarryOverType type)
        {
            _type = type;

            return this;
        }

        public ProfileCarryOverBuilder WithCarryOver(decimal carryOver)
        {
            _carryOver = carryOver;

            return this;
        }

        public ProfileCarryOverBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }
        
        public ProfilingCarryOver Build()
        {
            return new ProfilingCarryOver
            {
                Amount    = _carryOver.GetValueOrDefault(NewRandomNumberBetween(1, int.MaxValue)),
                Type = _type.GetValueOrDefault(NewRandomEnum(ProfilingCarryOverType.Undefined)),
                FundingLineCode = _fundingLineCode ?? NewRandomString()
            };
        }
    }
}