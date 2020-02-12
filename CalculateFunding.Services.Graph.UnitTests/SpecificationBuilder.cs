using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Graph.UnitTests
{
    public class SpecificationBuilder : TestEntityBuilder
    {
        private string _name;
        private string _description;
        private string _specificationId;

        public SpecificationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public SpecificationBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public SpecificationBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public Specification Build()
        {
            return new Specification
            {
                SpecificationId = _specificationId ?? new RandomString(),
                Name = _name ?? new RandomString(),
                Description = _description ?? new RandomString()
            };
        }
    }
}