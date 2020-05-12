using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Policy.Models;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface ITemplateBuilderService
    {
        Task<TemplateResponse> GetTemplate(string templateId);

        Task<TemplateResponse> GetTemplateVersion(string templateId, string version);

        Task<IEnumerable<TemplateResponse>> GetTemplateVersions(string templateId, List<TemplateStatus> statuses);
        
        Task<CommandResult> CreateTemplate(TemplateCreateCommand command, Reference author);

        Task<CommandResult> UpdateTemplateContent(TemplateContentUpdateCommand command, Reference author);

        Task<CommandResult> UpdateTemplateMetadata(TemplateMetadataUpdateCommand command, Reference author);
        
        Task<CommandResult> ApproveTemplate(Reference author, string templateId, string comment, string version = null);
    }
}