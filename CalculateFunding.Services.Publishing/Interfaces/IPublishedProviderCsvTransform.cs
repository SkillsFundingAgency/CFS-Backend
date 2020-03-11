using System.Collections.Generic;
using System.Dynamic;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderCsvTransform
    {
        IEnumerable<ExpandoObject> Transform(IEnumerable<dynamic> documents);
        bool IsForJobDefinition(string jobDefinitionName);
    }
}
