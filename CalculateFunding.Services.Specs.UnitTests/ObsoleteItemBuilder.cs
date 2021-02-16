using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class ObsoleteItemBuilder : TestEntityBuilder
    {
        private string _fundingLineId;
        private string _specificationId;
        private string[] _calculationIds = new string[0];
        private ObsoleteItemType? _itemType;

        public ObsoleteItemBuilder WithFundingLineId(string fundingLineId)
        {
            _fundingLineId = fundingLineId;
            return this;
        }

        public ObsoleteItemBuilder WithCalculationIds(params string[] calculationIds)
        {
            _calculationIds = calculationIds;
            return this;
        }
        public ObsoleteItemBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;
            return this;
        }

        public ObsoleteItemBuilder WithItemType(ObsoleteItemType? itemType)
        {
            _itemType = itemType;
            return this;
        }

        public ObsoleteItem Build()
        {
            return new ObsoleteItem()
            { 
                FundingLineId = _fundingLineId ?? NewRandomString(),
                CalculationIds = _calculationIds,
                SpecificationId = _specificationId ?? NewRandomString(),
                ItemType = _itemType ?? NewRandomEnum<ObsoleteItemType>()
            };
        }
    }
}