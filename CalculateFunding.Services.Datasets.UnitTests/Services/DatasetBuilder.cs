using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DatasetBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private string _description;
        private Reference _definition;
        private DatasetVersion _current;
        private IEnumerable<DatasetVersion> _history;

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

        public DatasetBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public DatasetBuilder WithCurrent(DatasetVersion current)
        {
            _current = current;

            return this;
        }

        public DatasetBuilder WithHistory(params DatasetVersion[] history)
        {
            _history = history;

            return this;
        }

        public DatasetBuilder WithDefinition(Reference definition)
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
                History = _history?.ToList(),
                Name = _name ?? NewRandomString(),
                Description = _description ?? NewRandomString()
            };
        }
    }
}