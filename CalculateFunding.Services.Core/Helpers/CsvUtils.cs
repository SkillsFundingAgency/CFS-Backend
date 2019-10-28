using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;

namespace CalculateFunding.Services.Core.Helpers
{
    public class CsvUtils : ICsvUtils
    {
        public string AsCsv(IEnumerable<dynamic> documents)
        {
            if (!documents.Any()) return "";

            using (StringWriter writer = new StringWriter())
            using (CsvWriter csvWriter = new CsvWriter(writer))
            {
                csvWriter.Configuration.ShouldQuote = (value, context) => true;
                csvWriter.Configuration.Quote = '\"';
                
                csvWriter.WriteRecords(documents);

                return writer.ToString();
            }
        }
    }
}