using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing
{
    public class InScopePublishedProviderService : IInScopePublishedProviderService
    {
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public InScopePublishedProviderService(IMapper mapper, ILogger logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _logger = logger;
            _mapper = mapper;
        }

        public Dictionary<string, PublishedProvider> GenerateMissingProviders(IEnumerable<ApiProvider> scopedProviders,
            SpecificationSummary specification,
            Reference fundingStream,
            Dictionary<string, PublishedProvider> publishedProviders,
            TemplateMetadataContents templateMetadataContents)
        {
            string specificationId = specification?.Id;
            string fundingPeriodId = specification?.FundingPeriod?.Id;
            string fundingStreamId = fundingStream?.Id;

            if (specificationId.IsNullOrWhitespace())
                LogErrorAndThrow("Could not locate a specification id on the supplied specification summary");

            if (fundingPeriodId.IsNullOrWhitespace())
                LogErrorAndThrow("Could not locate a funding period id on the supplied specification summary");

            if (fundingStreamId.IsNullOrWhitespace())
                LogErrorAndThrow("Could not locate a funding stream id from the supplied reference");

            return scopedProviders.Where(_ =>
                    !publishedProviders.ContainsKey($"{_.ProviderId}"))
                .Select(_ => new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        Version = 1,
                        MajorVersion = 1,
                        FundingPeriodId = fundingPeriodId,
                        FundingStreamId = fundingStreamId,
                        Status = _.Status.AsEnum<PublishedProviderStatus>(),
                        ProviderId = _.ProviderId,
                        Provider = _mapper.Map<Provider>(_),
                        SpecificationId = specificationId
                    }
                })
                .ToDictionary(_ => _.Current.ProviderId, _ => _);
        }

        private void LogErrorAndThrow(string exception)
        {
            _logger.Error(exception);

            throw new Exception(exception);
        }
    }
}