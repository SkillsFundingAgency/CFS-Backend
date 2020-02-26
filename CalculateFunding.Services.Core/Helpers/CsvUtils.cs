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
            if (!documents.Any()) return null;
            
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
    }
}