using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class TableLoadResultBuilder : TestEntityBuilder
    {
        private string _identifierType;
        private IEnumerable<RowLoadResult> _rows;

        public TableLoadResultBuilder WithIdentifierType(string identifierType)
        {
            _identifierType = identifierType;

            return this;
        }
        
        public TableLoadResultBuilder WithRows(params RowLoadResult[] rows)
        {
            _rows = rows;

            return this;
        }
        
        public TableLoadResult Build()
        {
            return new TableLoadResult
            {
                Rows = _rows?.ToList()
            };
        }
    }
}