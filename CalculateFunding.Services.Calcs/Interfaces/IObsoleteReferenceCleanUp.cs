using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IObsoleteReferenceCleanUp
    {
        Task ProcessCalculation(Calculation calculation);
    }
}