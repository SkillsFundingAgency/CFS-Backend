using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    public class DatasetReferenceBuilder : TestEntityBuilder
    {
        private string _propertyName;
        private IEnumerable<Calculation> _calculations;
        private DataField _dataField;
        private Dataset _dataset;
        private DatasetDefinition _datasetDefinition;

        public DatasetReferenceBuilder WithPropertyName(string propertyName)
        {
            _propertyName = propertyName;

            return this;
        }

        public DatasetReferenceBuilder WithCalculations(params Calculation[] calculations)
        {
            _calculations = calculations;

            return this;
        }

        public DatasetReferenceBuilder WithDataField(DataField dataField)
        {
            _dataField = dataField;

            return this;
        }

        public DatasetReferenceBuilder WithDataset(Dataset dataset)
        {
            _dataset = dataset;

            return this;
        }

        public DatasetReferenceBuilder WithDatasetDefinition(DatasetDefinition datasetDefinition)
        {
            _datasetDefinition = datasetDefinition;

            return this;
        }

        public DatasetReference Build()
        {
            return new DatasetReference
            {
                PropertyName = _propertyName ?? NewRandomString(),
                Calculations = _calculations?.ToList() ?? new List<Calculation>(),
                DataField = _dataField ?? new DataField()
                {
                    DataFieldId = NewRandomString(),
                    DataFieldName = NewRandomString()
                },
                Dataset = _dataset ?? new Dataset
                {
                    DatasetId = NewRandomString(),
                    Name = NewRandomString(),
                    Description = NewRandomString()
                },
                DatasetDefinition = _datasetDefinition ?? new DatasetDefinition
                {
                    DatasetDefinitionId = NewRandomString(),
                    Name = NewRandomString(),
                    Description = NewRandomString()
                }
            };
        }
    }
}
