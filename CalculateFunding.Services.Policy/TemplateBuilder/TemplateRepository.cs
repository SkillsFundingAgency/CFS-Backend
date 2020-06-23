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

        public async Task<Template> GetTemplate(string templateId)
        {
            Guard.IsNullOrWhiteSpace(templateId, nameof(templateId));

            IEnumerable<Template> templateMatches = await _cosmosRepository
                .Query<Template>(x => x.Content.TemplateId == templateId);

            if (templateMatches != null && templateMatches.Any())
            {
                if (templateMatches.Count() == 1)
                    return templateMatches.First();

                throw new ApplicationException($"Duplicate templates with TemplateId={templateId}");
            }

            return null;
        }

        public async Task<HttpStatusCode> CreateDraft(Template template)
        {
            return await _cosmosRepository.CreateAsync(template);
        }

        public async Task<HttpStatusCode> Update(Template template)
        {
            Guard.ArgumentNotNull(template, nameof(template));
            Guard.ArgumentNotNull(template.Id, nameof(template.Id));
            return await _cosmosRepository.UpdateAsync(template);
        }

        public async Task<bool> IsFundingStreamAndPeriodInUse(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            IEnumerable<Template> existing = await _cosmosRepository.Query<Template>(x =>
                x.Content.Current != null &&
                x.Content.Current.FundingStreamId == fundingStreamId &&
                x.Content.Current.FundingPeriodId == fundingPeriodId);

            return existing.Any();
        }

        public async Task GetTemplatesForIndexing(Func<List<Template>, Task> persistIndexBatch, int batchSize)
        {
            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"SELECT
        c.content.templateId,
        c.content.name,
        { 
           'name'         : c.content.name,
           'templateId'   : c.content.current.templateId,
           'date'         : c.content.current.date,
           'majorVersion' : c.content.current.majorVersion,
           'minorVersion' : c.content.current.minorVersion,
           'status'       : c.content.current.status,
           'version'      : c.content.current.version,
           'author'       : {
              'id'           : c.content.current.author.id,
              'name'         : c.content.current.author.name
            }
        } AS Current,
        { 
           'majorVersion' : c.content.released.majorVersion,
           'minorVersion' : c.content.released.minorVersion,
           'status'       : c.content.released.status,
           'version'      : c.content.released.version
        } AS Released,
        {
           'id'           : c.content.fundingStream.id,
           'name'         : c.content.fundingStream.name,
           'shortName'    : c.content.fundingStream.shortName
        } AS FundingStream,
        {
           'id'           : c.content.fundingPeriod.id,
           'name'         : c.content.fundingPeriod.name,
           'shortName'    : c.content.fundingPeriod.shortName
        } AS FundingPeriod
FROM     templateBuilder c
WHERE    c.documentType = 'Template' 
AND      c.deleted = false"
            };

            await _cosmosRepository.DocumentsBatchProcessingAsync(persistBatchToIndex: persistIndexBatch,
                cosmosDbQuery: query,
                itemsPerPage: batchSize);
        }

        public async Task<IEnumerable<Template>> GetAllTemplates()
        {
            IEnumerable<Template> templates = await _cosmosRepository.Query<Template>();

            return templates;
        }
    }
}