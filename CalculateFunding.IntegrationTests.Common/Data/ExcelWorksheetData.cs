using System.Collections.Generic;

namespace CalculateFunding.IntegrationTests.Common.Data
{
    public readonly struct ExcelWorksheetData
    {
        public ExcelWorksheetData(string name,
            string[] headers,
            params object[][] rows)
        {
            Name = name;
            Headers = headers;
            Rows = rows;
        }

        public string Name { get; }

        public string[] Headers { get; }

        public IEnumerable<object[]> Rows { get; }
    }
}