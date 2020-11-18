namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface ISqlNameGenerator
    {
        string GenerateIdentifier(string value);
    }
}