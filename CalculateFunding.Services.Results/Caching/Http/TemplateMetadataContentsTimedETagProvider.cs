using System;
using System.Linq;
using System.Threading.Tasks;
using CacheCow.Server;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CalculateFunding.Services.Results.Caching.Http
{
    public class TemplateMetadataContentsTimedETagProvider : ITimedETagQueryProvider<FundingStructure>
    {
        private readonly IFundingStructureService _fundingStructureService;

        public TemplateMetadataContentsTimedETagProvider(IFundingStructureService fundingStructureService)
        {
            Guard.ArgumentNotNull(fundingStructureService, nameof(fundingStructureService));

            _fundingStructureService = fundingStructureService;
        }

        public async Task<TimedEntityTagHeaderValue> QueryAsync(HttpContext context)
        {
            IQueryCollection queryData = context.Request.Query;

            string fundingStreamId = GetQueryData(nameof(fundingStreamId), queryData);
            string fundingPeriodId = GetQueryData(nameof(fundingPeriodId), queryData);
            string specificationId = GetQueryData(nameof(specificationId), queryData);

            DateTimeOffset fundingStructureLastModified = await _fundingStructureService.GetFundingStructureTimeStamp(fundingStreamId,
                fundingPeriodId,
                specificationId);

            string etag = fundingStructureLastModified.ToETagString();

            return new TimedEntityTagHeaderValue(etag);
        }

        private string GetQueryData(string key,
            IQueryCollection queryData)
            => queryData.TryGetValue(key, out StringValues value) ? value.FirstOrDefault()
                : throw new ArgumentOutOfRangeException(key, $"Expected route data to contain {key} parameter");

        public void Dispose()
        {
        }
    }
}