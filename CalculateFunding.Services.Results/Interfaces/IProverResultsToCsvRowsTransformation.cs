using System.Collections.Generic;
using System.Dynamic;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProverResultsToCsvRowsTransformation
    {
        IEnumerable<ExpandoObject> TransformProviderResultsIntoCsvRows(IEnumerable<ProviderResult> providerResults);
    }
}