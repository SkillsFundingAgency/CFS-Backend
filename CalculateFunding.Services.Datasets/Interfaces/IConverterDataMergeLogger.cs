using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets.Converter;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IConverterDataMergeLogger
    {
        Task SaveLogs(IEnumerable<RowCopyResult> results,
            ConverterMergeRequest request,
            string parentId,
            string jobId,
            int datasetVersionCreated);

        Task<IEnumerable<ConverterDataMergeLog>> GetLogs(string parentJobId);
        Task<ConverterDataMergeLog> GetLog(string jobId);
    }
}