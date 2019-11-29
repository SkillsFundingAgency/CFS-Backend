using System.Collections.Generic;

namespace CalculateFunding.Services.Core.Interfaces
{
    public interface ICsvUtils
    {
        string AsCsv(IEnumerable<object> documents, bool outputHeaders);
    }
}