using CalculateFunding.Common.Models;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IGeneratePublishedProviderEstateCsvJobsCreation
    {
        Task CreateJobs(string specificationId, string correlationId, Reference user);
    }
}
