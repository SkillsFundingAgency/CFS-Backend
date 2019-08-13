using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationCodeReferenceUpdate
    {
        string ReplaceSourceCodeReferences(Calculation calculation, string oldCalcSourceCodeName, string newCalcSourceCodeName);
    }
}