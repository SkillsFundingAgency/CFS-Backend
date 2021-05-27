using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IConverterActivityReportRepository
    {
        Task UploadReport(string filename, string prettyFilename, Stream csvFileStream, IDictionary<string, string> metadata);
    }
}
