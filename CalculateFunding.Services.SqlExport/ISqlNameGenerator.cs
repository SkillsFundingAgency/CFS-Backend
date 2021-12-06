namespace CalculateFunding.Services.SqlExport
{
    public interface ISqlNameGenerator
    {
        string GenerateIdentifier(string value);
    }
}