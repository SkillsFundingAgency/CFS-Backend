using System.Threading.Tasks;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IGeneratePublishedFundingCsvJobsCreation
    {
        Task CreateJobs(string specificationId, string correlationId, Reference user);
    }
}