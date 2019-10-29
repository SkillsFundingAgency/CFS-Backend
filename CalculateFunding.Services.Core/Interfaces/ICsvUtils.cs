using System.Collections.Generic;
using System.IO;

namespace CalculateFunding.Services.Core.Interfaces
{
    public interface ICsvUtils
    {
        StreamWriter AsCsvStream(IEnumerable<object> documents, bool outputHeaders);
        void ReturnStreamWriter(StreamWriter streamWriter);
    }
}