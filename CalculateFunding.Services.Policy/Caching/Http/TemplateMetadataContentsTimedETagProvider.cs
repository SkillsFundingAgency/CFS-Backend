using System;
using System.Threading.Tasks;
using CacheCow.Server;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Polly;

namespace CalculateFunding.Services.Policy.Caching.Http
{
    public class TemplateMetadataContentsTimedETagProvider : ITimedETagQueryProvider<TemplateMetadataContents>
    {
        private readonly AsyncPolicy _fundingTemplatesPolicy;
        private readonly IFundingTemplateRepository _fundingTemplates;

        public TemplateMetadataContentsTimedETagProvider(IFundingTemplateRepository fundingTemplates,
            IPolicyResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(fundingTemplates, nameof(fundingTemplates));
            Guard.ArgumentNotNull(resiliencePolicies?.FundingTemplateRepository, nameof(resiliencePolicies.FundingTemplateRepository));
            
            _fundingTemplates = fundingTemplates;
            _fundingTemplatesPolicy = resiliencePolicies.FundingTemplateRepository;
        }

        public async Task<TimedEntityTagHeaderValue> QueryAsync(HttpContext context)
        {
            RouteData routeData = context.GetRouteData();

            string fundingStreamId = GetRouteData(nameof(fundingStreamId), routeData);
            string fundingPeriodId = GetRouteData(nameof(fundingPeriodId), routeData);
            string templateVersion = GetRouteData(nameof(templateVersion), routeData);

            string blobName = new FundingTemplateVersionBlobName(fundingStreamId, fundingPeriodId, templateVersion);

            DateTimeOffset lastModified = await _fundingTemplatesPolicy.ExecuteAsync(() => _fundingTemplates.GetLastModifiedDate(blobName));

            string eTagString = lastModified.ToETagString();
            
            return new TimedEntityTagHeaderValue(eTagString);
        }

        private string GetRouteData(string key,
            RouteData routeData)
            => routeData.Values.TryGetValue(key, out object value) ? value?.ToString() 
                : throw new ArgumentOutOfRangeException(key, $"Expected route data to contain {key} parameter");

        public void Dispose()
        {
        }
    }
}