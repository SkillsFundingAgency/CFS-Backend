namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ITokenChecker
    {
        bool CheckIsToken(string sourceCode, string tokenName, int position);
    }
}