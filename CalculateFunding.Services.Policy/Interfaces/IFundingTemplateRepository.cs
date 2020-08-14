using System;
using CalculateFunding.Models.Policy;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingTemplateRepository
    {
        Task SaveFundingTemplateVersion(string blobName, byte[] templateBytes);

        Task<bool> TemplateVersionExists(string blobName);

        Task<string> GetFundingTemplateVersion(string blobName);

        Task<IEnumerable<PublishedFundingTemplate>> SearchTemplates(string blobNamePrefix);
        Task<DateTimeOffset> GetLastModifiedDate(string blobName);
    }
}
