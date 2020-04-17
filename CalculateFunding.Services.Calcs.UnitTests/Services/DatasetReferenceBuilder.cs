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
        private DatasetField _datasetField;

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

        public DatasetReferenceBuilder WithDatasetField(DatasetField datasetField)
        {
            _datasetField = datasetField;

            return this;
        }

        public DatasetReference Build()
        {
            return new DatasetReference
            {
                PropertyName = _propertyName ?? NewRandomString(),
                Calculations = _calculations?.ToList() ?? new List<Calculation>(),
                DatasetField = _datasetField ?? new DatasetField()
                {
                    DatasetFieldId = NewRandomString(),
                    DatasetFieldName = NewRandomString()
                }
            };
        }
    }
}
