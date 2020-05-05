using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Core.Services;

namespace CalculateFunding.Services.Policy.TemplateBuilder
{
    public class TemplateVersionRepository : VersionRepository<TemplateVersion>, ITemplateVersionRepository
    {
        public TemplateVersionRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository)
        {
        }

        public async Task<TemplateVersion> GetTemplateVersion(string templateId, int versionNumber)
        {
            Guard.IsNullOrWhiteSpace(templateId, nameof(templateId));

            IEnumerable<TemplateVersion> templateMatches = await _cosmosRepository.Query<TemplateVersion>(x => 
                x.Content.TemplateId == templateId && x.Content.Version == versionNumber);

            if (templateMatches != null && templateMatches.Any())
            {
                if (templateMatches.Count() == 1)
                    return templateMatches.First();
                
                throw new ApplicationException($"Duplicate templates with TemplateId={templateId}");
            }

            return null;
        }

        public async Task<TemplateVersion> GetTemplateVersions(string templateId, int versionNumber)
        {
            Guard.IsNullOrWhiteSpace(templateId, nameof(templateId));

            IEnumerable<TemplateVersion> templateMatches = await _cosmosRepository.Query<TemplateVersion>(x => 
                x.Content.TemplateId == templateId && x.Content.Version == versionNumber);

            if (templateMatches != null && templateMatches.Any())
            {
                if (templateMatches.Count() == 1)
                    return templateMatches.First();
                
                throw new ApplicationException($"Duplicate templates with TemplateId={templateId}");
            }

            return null;
        }
    }
}