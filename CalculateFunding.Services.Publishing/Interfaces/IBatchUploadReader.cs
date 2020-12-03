using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IBatchUploadReader
    {
        Task LoadBatchUpload(string blobName);
        
        bool HasPages { get; }
        int Count { get; }

        IEnumerable<string> NextPage();
    }
}