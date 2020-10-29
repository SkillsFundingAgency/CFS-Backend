using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public interface IApproveAllCalculationsJobAction
    {
        Task<Job> Run(string specificationId, Reference user, string correlationId);
    }
}