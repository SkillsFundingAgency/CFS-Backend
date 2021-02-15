using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IEnumReferenceCleanUp
    {
        Task ProcessCalculation(Calculation calculation);
    }
}