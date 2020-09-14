using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Code;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICodeContextBuilder
    {
        Task<IEnumerable<TypeInformation>> BuildCodeContextForSpecification(string specificationId);
    }
}