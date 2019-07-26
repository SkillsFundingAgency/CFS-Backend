using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Serilog;

namespace CalculateFunding.Api.External.V3.Services
{
    public class FundingService : IFundingService
    {
        private readonly IBlobClient _blobClient;
        private readonly ISearchRepository<PublishedFundingIndex> _fundingSearchRepository;
        private readonly Polly.Policy _publishedFundingRepositoryPolicy;
        private readonly Polly.Policy _fundingSearchRepositoryPolicy;
        private readonly ILogger _logger;

        public FundingService(IBlobClient blobClient, 
            ISearchRepository<PublishedFundingIndex> fundingSearchRepository,
            IPublishingResiliencePolicies policyResiliencePolicies,  
            ILogger logger)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(fundingSearchRepository, nameof(fundingSearchRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _blobClient = blobClient;
            _fundingSearchRepository = fundingSearchRepository;
            _fundingSearchRepositoryPolicy = policyResiliencePolicies.FundingFeedSearchRepository;
            _publishedFundingRepositoryPolicy = policyResiliencePolicies.PublishedFundingBlobRepository;
            _logger = logger;
        }

        public async Task<IActionResult> GetFundingByFundingResultId(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));

            PublishedFundingIndex fundingIndexedDocument = await _fundingSearchRepositoryPolicy.ExecuteAsync(() => _fundingSearchRepository.SearchById(id));

            string documentPath = string.Join("/", fundingIndexedDocument.DocumentPath.Split('/').Skip(4));

            ICloudBlob blob = _blobClient.GetBlockBlobReference(documentPath);

            if (!blob.Exists())
            {
                _logger.Error($"Failed to find blob with path: {documentPath}");
                return new NotFoundResult();
            }

            string fundingDocument = string.Empty;

            using (MemoryStream fundingDocumentStream = (MemoryStream)await _publishedFundingRepositoryPolicy.ExecuteAsync(() => _blobClient.DownloadToStreamAsync(blob)))
            {
                if (fundingDocumentStream == null || fundingDocumentStream.Length == 0)
                {
                    _logger.Error($"Invalid blob returned: {documentPath}");
                }

                fundingDocument = Encoding.UTF8.GetString(fundingDocumentStream.ToArray());
            }

            return new OkObjectResult(fundingDocument);
        }
    }
}
