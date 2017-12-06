using System.Collections.Generic;
using System.IO;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.DataImporter.Providers;
using CsvHelper;

namespace CalculateFunding.Services.DataImporter
{
    public class EdubaseImporterService
    {
        public IEnumerable<Provider> ImportEdubaseCsv(string name, StreamReader reader)
        {

            using (var csvReader = new CsvReader(reader))
            {
                csvReader.Configuration.HeaderValidated = null;
                csvReader.Configuration.MissingFieldFound = null;
                csvReader.Configuration.RegisterClassMap<EdubaseRecordMap>();
                var records = csvReader.GetRecords<Provider>();
                foreach (var record in records)
                {
                    yield return record;
                }
            }
        }
    }
}