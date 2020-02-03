using System;
using System.IO;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    internal class TempFile : IDisposable
    {
        private readonly string _path;

        public TempFile(string path, string contents)
        {
            _path = path;

            File.WriteAllText(_path, contents);
        }
        
        public FileInfo FileInfo => new FileInfo(_path);

        public void Dispose()
        {
            if (!File.Exists(_path)) return;

            try
            {
                File.Delete(_path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}