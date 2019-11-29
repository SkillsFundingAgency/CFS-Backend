using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public interface IFileSystemAccess
    {
        Stream OpenRead(string path);
        
        Task Write(string path, 
            Stream content, 
            CancellationToken cancellationToken = default);
        
        bool Exists(string path);
        
        void CreateFolder(string path);
        
        bool FolderExists(string path);

        void Delete(string path);

        Task Append(string path,
            Stream content,
            CancellationToken cancellationToken = default);

        Task Append(string path,
            string content,
            CancellationToken cancellationToken = default);
    }
}