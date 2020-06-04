using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Policy.TemplateBuilder;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface ITemplateRepository
    {
        Task<HttpStatusCode> CreateDraft(Template template);
        Task<Template> GetTemplate(string templateId);
        Task<HttpStatusCode> Update(Template template);
        Task<bool> IsFundingStreamAndPeriodInUse(string fundingStreamId, string fundingPeriodId);
        Task GetTemplatesForIndexing(Func<List<Template>, Task> persistIndexBatch, int batchSize);
        Task<IEnumerable<Template>> GetAllTemplates();
    }
}