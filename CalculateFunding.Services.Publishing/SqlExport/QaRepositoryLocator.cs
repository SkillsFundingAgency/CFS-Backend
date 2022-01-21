using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class QaRepositoryLocator : IQaRepositoryLocator
    {
        private readonly ConcurrentDictionary<SqlExportSource, IQaRepository> _qaRepositories;

        public QaRepositoryLocator(IEnumerable<IQaRepository> registeredQaRepositories)
        {
            _qaRepositories = new ConcurrentDictionary<SqlExportSource, IQaRepository>(
                registeredQaRepositories.ToDictionary(_ => _.SqlExportSource));
        }

        public IQaRepository GetService(SqlExportSource sqlExportSource)
        {
            if (_qaRepositories.TryGetValue(sqlExportSource, out IQaRepository qaRepository))
            {
                return qaRepository;
            }

            throw new ArgumentOutOfRangeException(nameof(sqlExportSource),
                $"Unable to find a registered QA Repository with name: {sqlExportSource}");
        }
    }
}
