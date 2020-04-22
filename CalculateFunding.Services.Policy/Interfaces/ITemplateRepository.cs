using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Policy.TemplateBuilder;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface ITemplateRepository
    {
        Task<HttpStatusCode> CreateDraft(Template template);
        Task<bool> IsTemplateNameInUse(string templateName);
    }
}