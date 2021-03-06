using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class FileSystemAccess : IFileSystemAccess
    {
        public Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public async Task Append(string path,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            await WriteStreamToFile(path, content, FileMode.Append, cancellationToken);
        }

        public async Task Append(string path,
            string content,
            CancellationToken cancellationToken = default)
        {
            using (FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(content);
                await writer.FlushAsync();
            }
        }

        public IEnumerable<string> GetAllFiles(string rootFolder, 
            Func<FileInfo, bool> predicate)
        {
            return new DirectoryInfo(rootFolder)
                .GetFiles("*", SearchOption.AllDirectories)
                .Where(predicate)
                .Select(_ => _.FullName)
                .ToArray();
        }

        public async Task Write(string path,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            await WriteStreamToFile(path, content, FileMode.CreateNew, cancellationToken);
        }

        private async Task WriteStreamToFile(string path,
            Stream content,
            FileMode fileMode,
            CancellationToken cancellationToken = default)
        {
            using (FileStream stream = new FileStream(path, fileMode, FileAccess.Write))
            {
                await content.CopyToAsync(stream, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
        }

        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public void CreateFolder(string path)
        {
            Directory.CreateDirectory(path);
        }

        public bool FolderExists(string path)
        {
            return Directory.Exists(path);
        }
    }
}