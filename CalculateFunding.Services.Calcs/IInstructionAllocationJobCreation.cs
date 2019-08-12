using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;

namespace CalculateFunding.Services.Calcs
{
    public interface IInstructionAllocationJobCreation
    {
        Task<Job> SendInstructAllocationsToJobService(string specificationId, string userId, string userName, Trigger trigger, string correlationId);
    }
}