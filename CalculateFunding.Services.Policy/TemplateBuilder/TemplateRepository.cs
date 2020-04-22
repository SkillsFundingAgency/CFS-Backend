using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Policy.Interfaces;

namespace CalculateFunding.Services.Policy.TemplateBuilder
{
    public class TemplateRepository : RepositoryBase, ITemplateRepository
    {
        public TemplateRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository)
        {
        }

        public async Task<HttpStatusCode> CreateDraft(Template template)
        {
            return await _cosmosRepository.CreateAsync(template);
        }

        public async Task<bool> IsTemplateNameInUse(string templateName)
        {
            Guard.IsNullOrWhiteSpace(templateName, nameof(templateName));

            IEnumerable<Template> existing = await _cosmosRepository.Query<Template>(x =>
                x.Content.Current != null &&
                x.Content.Current.Name.ToLower() == templateName.ToLower());

            return existing.Any();
        }
    }
}