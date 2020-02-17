using System.Threading.Tasks;
using CalculateFunding.Models.Graph;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ISpecificationCalculationAnalysis
    {
        Task<SpecificationCalculationRelationships> GetSpecificationCalculationRelationships(string specificationId);
    }
}