using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class BuildProjectBuilder : TestEntityBuilder
    {
        private string _id;
        private IEnumerable<DatasetRelationshipSummary> _relationships;

        public BuildProjectBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public BuildProjectBuilder WithRelationships(params DatasetRelationshipSummary[] relationships)
        {
            _relationships = relationships;

            return this;
        }
        
        public BuildProject Build()
        {
            return new BuildProject
            {
                Id = _id ?? NewRandomString(),
                DatasetRelationships = _relationships?.ToList()
            };
        }
    }
}