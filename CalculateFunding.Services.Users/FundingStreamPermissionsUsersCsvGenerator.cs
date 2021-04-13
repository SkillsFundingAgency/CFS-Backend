using CalculateFunding.Services.Users.Interfaces;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Common.Storage;
using Serilog;
using System.Threading.Tasks;
using System.Collections.Generic;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using System.Dynamic;
using System.Linq;
using Polly;

namespace CalculateFunding.Services.Users
{
    public class FundingStreamPermissionsUsersCsvGenerator : BaseUsersCsvGenerator, IUsersCsvGenerator
    {
        public const int BatchSize = 1000;

        private readonly IUserRepository _userRepository;
        private readonly AsyncPolicy _userRepositoryPolicy;

        public FundingStreamPermissionsUsersCsvGenerator(
            IFileSystemAccess fileSystemAccess,
            IBlobClient blobClient,
            ICsvUtils csvUtils,
            IFileSystemCacheSettings fileSystemCacheSettings,
            IUsersResiliencePolicies policies,
            IUsersCsvTransformServiceLocator usersCsvTransformServiceLocator,
            ILogger logger,
            IUserRepository userRepository) 
            : base(fileSystemAccess, blobClient, csvUtils, fileSystemCacheSettings, policies, usersCsvTransformServiceLocator, logger)
        {
            Guard.ArgumentNotNull(userRepository, nameof(userRepository));
            
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(policies.UserRepositoryPolicy, nameof(policies.UserRepositoryPolicy));

            _userRepository = userRepository;
            _userRepositoryPolicy = policies.UserRepositoryPolicy;
        }

        protected override string JobDefinitionName => JobConstants.DefinitionNames.GenerateFundingStreamPermissionsCsvJob;

        protected override async Task<bool> GenerateCsv(
            UserPermissionCsvGenerationMessage message, 
            string temporaryFilePath, 
            IUsersCsvTransform usersCsvTransform)
        {
            string fundingStreamId = message.FundingStreamId;
            IEnumerable<FundingStreamPermission> fundingStreamPermissions 
                = await _userRepositoryPolicy.ExecuteAsync(() => _userRepository.GetUsersWithFundingStreamPermissions(fundingStreamId));

            if (fundingStreamPermissions == null)
            {
                throw new NonRetriableException(
                    $"Unable to generate CSV for {JobConstants.DefinitionNames.GenerateFundingStreamPermissionsCsvJob} " +
                    $"for funding stream ID {fundingStreamId}. Failed to retrieve funding stream permissions items from repository");
            }

            IEnumerable<User> users = await _userRepositoryPolicy.ExecuteAsync(() => _userRepository.GetAllUsers());

            IEnumerable<UserFundingStreamPermission> userFundingStreamPermissions = fundingStreamPermissions.Select(_ => new UserFundingStreamPermission { 
                FundingStreamPermission = _,
                User = users.SingleOrDefault(u => u.Id == _.UserId)
            });

            IEnumerable<ExpandoObject> csvRows = usersCsvTransform.Transform(userFundingStreamPermissions);
            AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders: true);

            return true;
        }

        protected override string GetContentDisposition(UserPermissionCsvGenerationMessage message)
        {
            return $"attachment; filename={GetPrettyFileName(message)}";
        }

        protected override string GetPrettyFileName(UserPermissionCsvGenerationMessage message)
        {
            return $"CFS Permissions {message.Environment} {message.FundingStreamId} {message.ReportRunTime:s}.csv";
        }

        protected override string GetCsvFileName(UserPermissionCsvGenerationMessage message)
        {
            return $"permissions-funding-stream-{message.Environment}-{message.FundingStreamId}-{message.ReportRunTime:yyyy-MM-dd-HH-mm-ss}.csv";
        }

        protected override IDictionary<string, string> GetMetadata(UserPermissionCsvGenerationMessage message)
        {
            return new Dictionary<string, string>
            {
                { "funding-stream-id", message.FundingStreamId },
                { "environment", message.Environment },
            };
        }
    }
}
