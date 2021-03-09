using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Graph.UnitTests
{
    public class EnumBuilder : TestEntityBuilder
    {
        private string _name;
        private string _value;

        public EnumBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public EnumBuilder WithValue(string value)
        {
            _value = value;

            return this;
        }

        public Enum Build()
        {
            return new Enum
            {
                EnumName = _name ?? new RandomString(),
                EnumValue = _value ?? new RandomString()
            };
        }
    }
}