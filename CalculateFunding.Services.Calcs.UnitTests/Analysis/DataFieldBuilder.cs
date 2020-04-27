using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class DataFieldBuilder
    {
        private string _calculationId;
        private string _dataFieldId;
        private string _name;

        public DataFieldBuilder WithDataFieldId(string id)
        {
            _dataFieldId = id;

            return this;
        }

        public DataFieldBuilder WithCalculationId(string id)
        {
            _calculationId = id;

            return this;
        }

        public DataFieldBuilder WithDatasetName(string name)
        {
            _name = name;
            return this;
        }

        public DataField Build()
        {
            return new DataField
            {
                CalculationId = _calculationId ?? new RandomString(),
                DataFieldId = _dataFieldId ?? new RandomString(),
                DataFieldName = _name ?? new RandomString()
            };
        }
    }
}
