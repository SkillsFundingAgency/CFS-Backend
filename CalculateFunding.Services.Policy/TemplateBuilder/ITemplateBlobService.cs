using System.Threading.Tasks;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Policy.Models;

namespace CalculateFunding.Services.Policy.TemplateBuilder
{
    public interface ITemplateBlobService
    {
        Task<CommandResult> PublishTemplate(Template template);
    }
}