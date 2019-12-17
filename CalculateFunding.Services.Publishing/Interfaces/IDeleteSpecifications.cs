using System.Threading.Tasks;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDeleteSpecifications
    {
        Task QueueDeleteSpecificationJob(string specificationId,
            Reference user,
            string correlationId);
    }
}