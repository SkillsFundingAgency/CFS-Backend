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
        /// Returns a pooled stream writer for the supplied documents transformed into CVS rows
        /// NB the calling code is responsible for returning stream writer after use to the
        /// csv utils
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