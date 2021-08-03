using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DatasetBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private DatasetDefinitionVersion _definition;
        private DatasetVersion _current;

        public DatasetBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public DatasetBuilder WithName(string name)
        {
            _name = name;

            return this;
        }
        public DatasetBuilder WithCurrent(DatasetVersion current)
        {
            _current = current;

            return this;
        }
        public DatasetBuilder WithDefinition(DatasetDefinitionVersion definition)
        {
            _definition = definition;

            return this;
        }

        public Dataset Build()
        {
            return new Dataset
            {
                Id = _id ?? NewRandomString(),
                Definition = _definition,
                Current = _current,
                Name = _name ?? NewRandomString()
            };
        }
    }
}