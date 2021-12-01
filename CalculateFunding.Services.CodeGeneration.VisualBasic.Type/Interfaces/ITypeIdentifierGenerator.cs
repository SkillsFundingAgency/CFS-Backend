namespace CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces
{
    public interface ITypeIdentifierGenerator
    {
        string EscapeReservedWord(string value);
        string GenerateIdentifier(string value, bool escapeLeadingNumber = true);
    }
}
