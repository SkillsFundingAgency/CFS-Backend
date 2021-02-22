using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Calcs.Analysis.ObsoleteItems
{
    public interface IObsoleteItemCleanup
    {
        Task ProcessCalculation(Calculation calculation);
    }
}