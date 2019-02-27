using System.IO;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class StreamExtensions
    {
        public static byte[] ReadAllBytes(this Stream instream)
        {
            if(instream == null || instream.Length == 0)
            {
                return new byte[0];
            }

            if (instream is MemoryStream)
            {
                return ((MemoryStream)instream).ToArray();
            }

            using (var memoryStream = new MemoryStream())
            {
                instream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
