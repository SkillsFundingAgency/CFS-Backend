using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ICalculationsService
    {
        Task<bool> HaveAllTemplateCalculationsBeenApproved(string specificationId);
    }
}
