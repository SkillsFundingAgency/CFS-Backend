using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class ReProfileResponseBuilder : TestEntityBuilder
    {
        private decimal _carryOverAmount;
        private IEnumerable<DeliveryProfilePeriod> _deliveryProfilePeriods;

        public ReProfileResponseBuilder WithCarryOverAmount(decimal carryOverAmount)
        {
            _carryOverAmount = carryOverAmount;

            return this;
        }

        public ReProfileResponseBuilder WithDeliveryProfilePeriods(params DeliveryProfilePeriod[] deliveryProfilePeriods)
        {
            _deliveryProfilePeriods = deliveryProfilePeriods;

            return this;
        }
        
        public ReProfileResponse Build()
        {
            return new ReProfileResponse
            {
                CarryOverAmount = _carryOverAmount,
                DeliveryProfilePeriods = _deliveryProfilePeriods?.ToArray()
            };
        }
    }
}