namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationCodeReferenceUpdate
    {
        string ReplaceSourceCodeReferences(string sourceCode, string oldCalcSourceCodeName, string newCalcSourceCodeName);
    }
}