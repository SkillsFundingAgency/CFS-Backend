namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ITokenChecker
    {
        int? CheckIsToken(string sourceCode, string tokenNamespace, string tokenName, int position);
        int? CheckIsToken(string sourceCode, string tokenName, bool isNamespaced, int position);
        void CheckTokenLegal(string tokenName, bool isNamespaced);
    }
}