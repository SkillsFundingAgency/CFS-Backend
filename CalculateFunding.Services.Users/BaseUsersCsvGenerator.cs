using CalculateFunding.Services.Users.Interfaces;
using System.Collections.Generic;
using System.Dynamic;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Caching.FileSystem;
using System.Threading.Tasks;
using System.IO;
using CalculateFunding.Common.Storage;
using Polly;
using CalculateFunding.Common.Utility;
using Microsoft.Azure.Storage.Blob;
using Serilog;
using CalculateFunding.Models.Users;
using System;

namespace CalculateFunding.Services.Users
{
    public abstract class BaseUsersCsvGenerator : IUsersCsvGenerator
    {
        private const string UsersReportContainerName = "userreports";

        private readonly ICsvUtils _csvUtils;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IFileSystemCacheSettings _fileSystemCacheSettings;
        private readonly IBlobClient _blobClient;
        private readonly AsyncPolicy _blobClientPolicy;
        private readonly IUsersCsvTransformServiceLocator _usersCsvTransformServiceLocator;
        private readonly ILogger _logger;

        protected BaseUsersCsvGenerator(
            IFileSystemAccess fileSystemAccess,
            IBlobClient blobClient,
            ICsvUtils csvUtils,
            IFileSystemCacheSettings fileSystemCacheSettings,
            IUsersResiliencePolicies policies,
            IUsersCsvTransformServiceLocator usersCsvTransformServiceLocator,
            ILogger logger)
        {
            Guard.ArgumentNotNull(fileSystemAccess, nameof(fileSystemAccess));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(csvUtils, nameof(csvUtils));
            Guard.ArgumentNotNull(fileSystemCacheSettings, nameof(fileSystemCacheSettings));
            Guard.ArgumentNotNull(usersCsvTransformServiceLocator, nameof(usersCsvTransformServiceLocator));
            Guard.ArgumentNotNull(logger, nameof(logger));

            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(policies.BlobClient, nameof(policies.BlobClient));

            _fileSystemAccess = fileSystemAccess;
            _blobClient = blobClient;
            _blobClientPolicy = policies.BlobClient;
            _csvUtils = csvUtils;
            _fileSystemCacheSettings = fileSystemCacheSettings;
            _usersCsvTransformServiceLocator = usersCsvTransformServiceLocator;
            _logger = logger;
        }

        protected abstract string JobDefinitionName { get; }

        public async Task<FundingStreamPermissionCurrentDownloadModel> Generate(UserPermissionCsvGenerationMessage message)
        {
            string temporaryFilePath = GetCsvFilePath(_fileSystemCacheSettings.Path, GetCsvFileName(message));

            EnsureFileIsNew(temporaryFilePath);

            IUsersCsvTransform usersCsvTransform = _usersCsvTransformServiceLocator.GetService(JobDefinitionName);
            bool processedResults = await GenerateCsv(message, temporaryFilePath, usersCsvTransform);

            if (!processedResults)
            {
                _logger.Information("Did not create a new csv report as no user permissions matched");

                return null;
            }

            string fileName = GetCsvFileName(message);
            string prettyFileName = GetPrettyFileName(message);

            await UploadToBlob(
                temporaryFilePath, 
                GetCsvFileName(message), 
                GetContentDisposition(message), 
                GetMetadata(message));

            string blobUrl = _blobClient.GetBlobSasUrl(
                fileName, 
                DateTimeOffset.Now.AddDays(1), 
                SharedAccessBlobPermissions.Read, 
                UsersReportContainerName);

            return new FundingStreamPermissionCurrentDownloadModel
            {
                FileName = prettyFileName,
                Url = blobUrl
            };
        }

        protected abstract string GetCsvFileName(UserPermissionCsvGenerationMessage message);

        protected abstract string GetPrettyFileName(UserPermissionCsvGenerationMessage message);

        protected abstract string GetContentDisposition(UserPermissionCsvGenerationMessage message);

        protected abstract IDictionary<string, string> GetMetadata(UserPermissionCsvGenerationMessage message);

        protected abstract Task<bool> GenerateCsv(UserPermissionCsvGenerationMessage message, string temporaryFilePath, IUsersCsvTransform usersCsvTransform);

        public void AppendCsvFragment(string temporaryFilePath, IEnumerable<ExpandoObject> csvRows, bool outputHeaders)
        {
            string csv = _csvUtils.AsCsv(csvRows, outputHeaders);

            _fileSystemAccess.Append(temporaryFilePath, csv)
                .GetAwaiter()
                .GetResult();
        }

        private void EnsureFileIsNew(string path)
        {
            if (_fileSystemAccess.Exists(path))
            {
                _fileSystemAccess.Delete(path);
            }
        }

        private async Task UploadToBlob(string temporaryFilePath, string blobPath, string contentDisposition, IDictionary<string, string> metadata)
        {
            ICloudBlob blob = _blobClient.GetBlockBlobReference(blobPath, UsersReportContainerName);
            blob.Properties.ContentDisposition = contentDisposition;

            using Stream csvFileStream = _fileSystemAccess.OpenRead(temporaryFilePath);
            await _blobClientPolicy.ExecuteAsync(() => UploadBlob(blob, csvFileStream, metadata));
        }

        private async Task UploadBlob(ICloudBlob blob, Stream csvFileStream, IDictionary<string, string> metadata)
        {
            await _blobClient.UploadFileAsync(blob, csvFileStream);
            await _blobClient.AddMetadataAsync(blob, metadata);
        }

        private string GetCsvFilePath(string rootPath, string fileName)
        {
            return Path.Combine(rootPath, fileName);
        }
    }
}
