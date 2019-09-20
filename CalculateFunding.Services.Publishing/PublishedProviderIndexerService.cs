using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderIndexerService : IPublishedProviderIndexerService
    {
        private readonly ISearchRepository<PublishedProviderIndex> _searchRepository;
        private readonly ILogger _logger;
        

        public PublishedProviderIndexerService(          
            ILogger logger,          
            ISearchRepository<PublishedProviderIndex> searchRepository          
            )
        {           
            Guard.ArgumentNotNull(logger, nameof(logger));           
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));           

            _logger = logger;       
            _searchRepository = searchRepository;          
        }

        public async Task IndexPublishedProvider(PublishedProviderVersion publishedProviderVersion)
        {
            if (publishedProviderVersion == null)
            {
                string error = "Null published provider version supplied";
                _logger.Error(error);
                throw new NonRetriableException(error);
            }

            IEnumerable<IndexError> publishedProviderIndexingErrors = await _searchRepository.Index(new[]
             {
                CreatePublishedProviderIndex(publishedProviderVersion)
            });

            List<IndexError> publishedProviderIndexingErrorsAsList = publishedProviderIndexingErrors.ToList();
            if (!publishedProviderIndexingErrorsAsList.IsNullOrEmpty())
            {
                string publishedProviderIndexingErrorsConcatted = string.Join(". ", publishedProviderIndexingErrorsAsList.Select(e => e.ErrorMessage));
                string formattedErrorMessage =
                    $"Could not index Published Provider {publishedProviderVersion.Id} because: {publishedProviderIndexingErrorsConcatted}";
                _logger.Error(formattedErrorMessage);
                throw new RetriableException(formattedErrorMessage);
            }

        }

        private PublishedProviderIndex CreatePublishedProviderIndex(PublishedProviderVersion publishedProviderVersion)
        {          

            return new PublishedProviderIndex
            {
                Id = publishedProviderVersion.Id,
                ProviderType = publishedProviderVersion.Provider.ProviderType,
                LocalAuthority = publishedProviderVersion.Provider.LocalAuthorityName,
                FundingStatus = publishedProviderVersion.Status.ToString(),
                ProviderName = publishedProviderVersion.Provider.Name,                
                UKPRN = publishedProviderVersion.Provider.UKPRN,               
                FundingValue = Convert.ToDouble(publishedProviderVersion.TotalFunding),
                SpecificationId = publishedProviderVersion.SpecificationId,
                FundingStreamIds = new string[]{ publishedProviderVersion.FundingStreamId },
                FundingPeriodId = publishedProviderVersion.FundingPeriodId

            };
        }
    }
}
