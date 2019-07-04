using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingTemplateRepository
    {
        Task SaveFundingTemplateVersion(string blobName, byte[] templateBytes);

        Task<bool> TemplateVersionExists(string blobName);

        Task<string> GetFundingTemplateVersion(string blobName);
    }
}
