using System;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface ISpecificationRepository
    {
        Task<HttpStatusCode> UpdateCalculationLastUpdatedDate(string specificationId);
    }

}
