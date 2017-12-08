using System.Collections.Generic;
using System.IO;
using CalculateFunding.Models.Providers;
using CsvHelper;

namespace CalculateFunding.Repositories.Providers
{
    public class ProviderEntity : ProviderBaseEntity
    {
    }

    public class EdubaseImporterService
    {
        public IEnumerable<ProviderCommandCandidateEntity> ImportEdubaseCsv(string name, StreamReader reader)
        {

            using (var csvReader = new CsvReader(reader))
            {
                csvReader.Configuration.HeaderValidated = null;
                csvReader.Configuration.MissingFieldFound = null;
                csvReader.Configuration.RegisterClassMap<EdubaseRecordMap>();
                var records = csvReader.GetRecords<ProviderCommandCandidateEntity>();
                foreach (var record in records)
                {
                    yield return record;
                }
            }
        }
    }
}