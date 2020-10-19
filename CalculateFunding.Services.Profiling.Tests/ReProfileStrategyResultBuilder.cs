using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class ReProfileStrategyResultBuilder : TestEntityBuilder
    {
        private IEnumerable<DeliveryProfilePeriod> _deliveryProfilePeriods;
        private IEnumerable<DistributionPeriods> _distributionPeriods;
        private decimal _carryOverAmount;

        public ReProfileStrategyResultBuilder WithDeliveryProfilePeriods(params DeliveryProfilePeriod[] deliveryProfilePeriods)
        {
            _deliveryProfilePeriods = deliveryProfilePeriods;

            return this;
        }

        public ReProfileStrategyResultBuilder WithDistributionPeriods(params DistributionPeriods[] distributionPeriods)
        {
            _distributionPeriods = distributionPeriods;

            return this;
        }

        public ReProfileStrategyResultBuilder WithCarryOverAmount(decimal carryOverAmount)
        {
            _carryOverAmount = carryOverAmount;

            return this;
        }

        public ReProfileStrategyResult Build()
        {
            return new ReProfileStrategyResult
            {
                DistributionPeriods = _distributionPeriods?.ToArray(),
                CarryOverAmount = _carryOverAmount,
                DeliveryProfilePeriods = _deliveryProfilePeriods?.ToArray()
            };
        }
    }
}