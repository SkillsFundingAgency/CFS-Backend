using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedProviderFundingStructureItemBuilder: TestEntityBuilder
    {
        private int _level;
        private string _name;
        private string _fundingLineCode;
        private string _calculationId;
        private PublishedProviderFundingStructureType _type;
        private string _value;
        private string _calculationType;
        private List<PublishedProviderFundingStructureItem> _fundingStructureItems;

        public PublishedProviderFundingStructureItemBuilder WithLevel(int level)
        {
            _level = level;

            return this;
        }

        public PublishedProviderFundingStructureItemBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public PublishedProviderFundingStructureItemBuilder WithCalculationType(string calculationType)
        {
            _calculationType = calculationType;

            return this;
        }

        public PublishedProviderFundingStructureItemBuilder WithType(PublishedProviderFundingStructureType type)
        {
            _type = type;

            return this;
        }

        public PublishedProviderFundingStructureItemBuilder WithValue(string value)
        {
            _value = value;

            return this;
        }

        public PublishedProviderFundingStructureItemBuilder WithFundingStructureItems(List<PublishedProviderFundingStructureItem> fundingStructureItems)
        {
            _fundingStructureItems = fundingStructureItems;

            return this;
        }

        public PublishedProviderFundingStructureItem Build()
        {

            return new PublishedProviderFundingStructureItem(
                _level, 
                _name, 
                _fundingLineCode,
                _calculationId, 
                _type, 
                _value, 
                _calculationType, 
                _fundingStructureItems);
        }
    }
}