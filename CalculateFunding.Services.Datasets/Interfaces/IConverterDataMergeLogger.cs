using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets.Converter;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IConverterDataMergeLogger
    {
        Task SaveLogs(IEnumerable<RowCopyResult> results,
            ConverterMergeRequest request,
            string jobId,
            int datasetVersionCreated);
    }
}