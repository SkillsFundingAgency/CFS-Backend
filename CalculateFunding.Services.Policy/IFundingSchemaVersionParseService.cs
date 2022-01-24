using CalculateFunding.Services.Policy.Models;

namespace CalculateFunding.Services.Policy
{
    public interface IFundingSchemaVersionParseService
    {
        string GetInputTemplateSchemaVersion(string templateContents);
    }
}