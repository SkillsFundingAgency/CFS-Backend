using System.Collections.Generic;
using System.IO;
using CalculateFunding.Models.Datasets;
using CsvHelper;

namespace CalculateFunding.Repositories.Providers
{
    public class ProviderEntity : ProviderBaseEntity
    {
    }

    public class EdubaseImporterService
    {
        public IEnumerable<Provider> ImportEdubaseCsv(StreamReader reader)
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