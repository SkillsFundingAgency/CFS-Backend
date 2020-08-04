using System.Collections.Generic;
using System.Dynamic;
using CalculateFunding.Models.Calcs;


namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderResultsToCsvRowsTransformation
    {
        IEnumerable<ExpandoObject> TransformProviderResultsIntoCsvRows(IEnumerable<ProviderResult> providerResults);
    }
}