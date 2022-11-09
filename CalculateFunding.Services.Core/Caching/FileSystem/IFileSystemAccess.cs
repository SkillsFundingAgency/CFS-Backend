using System;
using System.Collections.Generic;
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

        Task Write(string path,
            string content,
            CancellationToken cancellationToken = default);

        Task WritePoco<TPoco>(string path,
            TPoco content,
            bool useCamelCase = true,
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

        IEnumerable<string> GetAllFiles(string rootFolder, 
            Func<FileInfo, bool> predicate);
    }
}