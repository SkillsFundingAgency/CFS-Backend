using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class PublishingDataTableImporterLocator : IPublishingDataTableImporterLocator
    {
        private readonly ConcurrentDictionary<SqlExportSource, IPublishingDataTableImporter> _qaRepositories;

        public PublishingDataTableImporterLocator(IEnumerable<IPublishingDataTableImporter> registeredDataTableImporters)
        {
            _qaRepositories = new ConcurrentDictionary<SqlExportSource, IPublishingDataTableImporter>(
                registeredDataTableImporters.ToDictionary(_ => _.SqlExportSource));
        }

        public IPublishingDataTableImporter GetService(SqlExportSource sqlExportSource)
        {
            if (_qaRepositories.TryGetValue(sqlExportSource, out IPublishingDataTableImporter dataTableImporter))
            {
                return dataTableImporter;
            }

            throw new ArgumentOutOfRangeException(nameof(sqlExportSource),
                $"Unable to find a registered Publishing DataTableImporter with name: {sqlExportSource}");
        }
    }
}
