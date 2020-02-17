using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class GraphSpecificationBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private string _description;
        
        
        public GraphSpecificationBuilder WithId(string id)
        {
            _id = id;

            return this;
        }
        
        public GraphSpecificationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }
        
        public GraphSpecificationBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }
        
        public Specification Build()
        {
            return new Specification
            {
                SpecificationId = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Description = _description ?? NewRandomString()
            };
        }
    }
}