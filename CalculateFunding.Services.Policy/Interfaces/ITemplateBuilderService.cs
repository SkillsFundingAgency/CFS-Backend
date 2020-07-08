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

        Task<TemplateVersionListResponse> FindTemplateVersions(string templateId, List<TemplateStatus> statuses, int page, int itemsPerPage);

        Task<IEnumerable<TemplateSummaryResponse>> FindVersionsByFundingStreamAndPeriod(FindTemplateVersionQuery query);
        
        Task<CommandResult> CreateTemplate(TemplateCreateCommand command, Reference author);

        Task<CommandResult> UpdateTemplateContent(TemplateFundingLinesUpdateCommand originalCommand, Reference author);

        Task<CommandResult> RestoreTemplateContent(TemplateFundingLinesUpdateCommand originalCommand, Reference author);

        Task<CommandResult> UpdateTemplateDescription(TemplateDescriptionUpdateCommand command, Reference author);

        Task<CommandResult> PublishTemplate(TemplatePublishCommand command);

        Task<CommandResult> CreateTemplateAsClone(TemplateCreateAsCloneCommand command, Reference author);
        
        Task<IEnumerable<FundingStreamWithPeriods>> GetFundingStreamAndPeriodsWithoutTemplates();
    }
}