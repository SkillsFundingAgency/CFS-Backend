using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Messages;

namespace CalculateFunding.Services.Specs
{
    public interface IQueueDeleteSpecificationJobActions
    {
        Task Run(string specificationId, Reference user, string correlationId, DeletionType deletionType);
    }
}