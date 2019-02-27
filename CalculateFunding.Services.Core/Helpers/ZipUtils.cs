using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;

namespace CalculateFunding.Services.Core.Helpers
{
    public static class ZipUtils
    {
        public static byte[] ZipFiles(IEnumerable<(string filename, string content)> files)
        {
            if (files.IsNullOrEmpty())
            {
                return new byte[0];
            }

            using (MemoryStream zipStream = new MemoryStream())
            {
                using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {

                    foreach ((string filename, string content) item in files)
                    {
                        ZipArchiveEntry entry = zip.CreateEntry(item.filename);
                        using (Stream entryStream = entry.Open())
                        {
                            using (StreamWriter writer = new StreamWriter(entryStream))
                            {
                                writer.Write(item.content);

                            }
                        }
                    }
                } 

                return zipStream.ToArray();
            }
        }
    }
}
