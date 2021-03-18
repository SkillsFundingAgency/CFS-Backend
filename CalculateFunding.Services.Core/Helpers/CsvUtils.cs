using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalculateFunding.Services.Core.Interfaces;
using CsvHelper;

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
            
            using (StringWriter writer = new StringWriter())
            using (CsvWriter csvWriter = new CsvWriter(writer))
            {
                csvWriter.Configuration.ShouldQuote = (value, context) => true;
                csvWriter.Configuration.Quote = '\"';
                csvWriter.Configuration.HasHeaderRecord = outputHeaders;

                csvWriter.WriteRecords(documents);
                
                return writer.ToString();
            }
        }

        public IEnumerable<TPoco> AsPocos<TPoco>(string csv, string dateTimeFormat = "dd/MM/yyyy")
        {
            if (csv.IsNullOrWhitespace()) return new TPoco[0];
            
            using (StringReader stringReader = new StringReader(csv))
            using (CsvReader csvReader = new CsvReader(stringReader))
            {
                csvReader.Configuration.Quote = '\"';
                csvReader.Configuration.HasHeaderRecord = true;
                csvReader.Configuration.TypeConverterOptionsCache
                    .GetOptions<DateTimeOffset>()
                    .Formats = new[] {dateTimeFormat};
                
                return csvReader.GetRecords<TPoco>()
                    .ToArray();
            }
        }
    }
}