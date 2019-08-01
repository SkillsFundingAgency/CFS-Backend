using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Api.External.V3.Services
{
    public class PublishedFundingRetrievalService : IPublishedFundingRetrievalService
    {
        private readonly IBlobClient _blobClient;
        private Policy _publishedFundingRepositoryPolicy;
        private readonly ILogger _logger;

        public PublishedFundingRetrievalService(IBlobClient blobClient,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _blobClient = blobClient;
            _publishedFundingRepositoryPolicy = resiliencePolicies.PublishedFundingBlobRepository;
            _logger = logger;
        }

        public async Task<string> GetFundingFeedDocument(string absoluteDocumentPathUrl)
        {
            Guard.IsNullOrWhiteSpace(absoluteDocumentPathUrl, nameof(absoluteDocumentPathUrl));

            string documentPath = ParseDocumentPathRelativeToBlobContainerFromFullUrl(absoluteDocumentPathUrl);

            ICloudBlob blob = _blobClient.GetBlockBlobReference(documentPath);

            if (!blob.Exists())
            {
                _logger.Error($"Failed to find blob with path: {documentPath}");
                return null;
            }

            using (MemoryStream fundingDocumentStream = (MemoryStream)await _publishedFundingRepositoryPolicy.ExecuteAsync(() => _blobClient.DownloadToStreamAsync(blob)))
            {
                if (fundingDocumentStream == null || fundingDocumentStream.Length == 0)
                {
                    _logger.Error($"Invalid blob returned: {documentPath}");
                    return null;
                }

                return Encoding.UTF8.GetString(fundingDocumentStream.ToArray());
            }
        }

        /// <summary>
        /// Converts the full URL to the document from a storage URL, 
        /// eg https://strgt1dvprovcfs.blob.core.windows.net/publishedfunding/subfolder/PES-AY-1920-Payment-LocalAuthority-12345678-1_0.json to subfolder/PES-AY-1920-Payment-LocalAuthority-12345678-1_0.json
        /// </summary>
        /// <param name="documentPath">Document Path URL</param>
        /// <returns>Relative path from the container, without a leading /</returns>
        public string ParseDocumentPathRelativeToBlobContainerFromFullUrl(string documentPath)
        {
            Uri uri = new Uri(documentPath);
            documentPath = uri.AbsolutePath;
            if (!string.IsNullOrWhiteSpace(documentPath))
            {
                if (documentPath.StartsWith("/"))
                {
                    documentPath = documentPath.Substring(1, documentPath.Length - 1);
                }

                int firstForwardSlash = documentPath.IndexOf("/");

                documentPath = documentPath.Substring(firstForwardSlash + 1, documentPath.Length - firstForwardSlash - 1);
            }

            return documentPath;
        }
    }
}
