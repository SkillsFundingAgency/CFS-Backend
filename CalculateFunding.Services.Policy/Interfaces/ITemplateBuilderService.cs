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

        Task<IEnumerable<TemplateResponse>> GetVersionsByTemplate(string templateId, List<TemplateStatus> statuses);

        Task<IEnumerable<TemplateResponse>> FindVersionsByFundingStreamAndPeriod(FindTemplateVersionQuery query);
        
        Task<CommandResult> CreateTemplate(TemplateCreateCommand command, Reference author);

        Task<CommandResult> UpdateTemplateContent(TemplateContentUpdateCommand command, Reference author);

        Task<CommandResult> UpdateTemplateMetadata(TemplateMetadataUpdateCommand command, Reference author);
        
        Task<CommandResult> ApproveTemplate(Reference author, string templateId, string comment, string version = null);
        
        Task<CommandResult> CreateTemplateAsClone(TemplateCreateAsCloneCommand command, Reference author);
    }
}