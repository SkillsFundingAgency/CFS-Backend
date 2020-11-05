using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IRecordVariationErrors
    {
        Task RecordVariationErrors(IEnumerable<string> variationErrors, string specificationId, string jobId);
    }
}