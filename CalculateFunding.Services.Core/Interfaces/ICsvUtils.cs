using System.Collections.Generic;

namespace CalculateFunding.Services.Core.Helpers
{
    public interface ICsvUtils
    {
        string AsCsv(IEnumerable<object> documents);
    }
}