using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalculateFunding.Services.Core.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;

namespace CalculateFunding.Services.Core.Helpers
{
    public class CsvUtils : ICsvUtils
    {
        /// <summary>
        /// Returns the supplied documents as a Csv string
        /// </summary>
        public string AsCsv(IEnumerable<dynamic> documents, bool outputHeaders)
        {
            if (!documents.AnyWithNullCheck()) return null;

            CsvConfiguration csvConfiguration = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                Quote = '\"',
                HasHeaderRecord = outputHeaders,
                ShouldQuote = args => true
            };

            using StringWriter writer = new StringWriter();
            using CsvWriter csvWriter = new CsvWriter(writer, csvConfiguration);

            csvWriter.WriteRecords(documents);

            return writer.ToString();
        }

        public IEnumerable<TPoco> AsPocos<TPoco>(string csv, string dateTimeFormat = "dd/MM/yyyy")
        {
            if (csv.IsNullOrWhitespace()) return Array.Empty<TPoco>();

            CsvConfiguration csvConfiguration = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                Quote = '\"',
                HasHeaderRecord = true,
            };

            using StringReader stringReader = new StringReader(csv);
            using CsvReader csvReader = new CsvReader(stringReader, csvConfiguration);
            csvReader.Context.TypeConverterOptionsCache
                .GetOptions<DateTimeOffset>()
                .Formats = new[] { dateTimeFormat };

            return csvReader.GetRecords<TPoco>()
                .ToArray();
        }
    }
}