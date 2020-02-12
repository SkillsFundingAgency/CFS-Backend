using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Policies.Models.ViewModels;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using Serilog;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class PoliciesInMemoryRepository : IPoliciesApiClient
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, FundingConfiguration> _fundingConfigurations = new Dictionary<string, FundingConfiguration>();
        private readonly Dictionary<string, FundingPeriod> _fundingPeriods = new Dictionary<string, FundingPeriod>();

        public PoliciesInMemoryRepository(ILogger logger)
        {
            _logger = logger;
        }

        public void SetFundingConfiguration(string fundingStreamId, string fundingPeriodId,
            FundingConfiguration fundingConfiguration)
        {
            _fundingConfigurations[$"{fundingStreamId}-{fundingPeriodId}"] = fundingConfiguration;
        }

        public Task<ApiResponse<FundingConfiguration>> GetFundingConfiguration(string fundingStreamId, string fundingPeriodId)
        {
            string fundingConfigurationKey = $"{fundingStreamId}-{fundingPeriodId}";

            _fundingConfigurations.TryGetValue(fundingConfigurationKey, out FundingConfiguration fundingConfiguration);

            return Task.FromResult(fundingConfiguration == null ? 
                new ApiResponse<FundingConfiguration>(System.Net.HttpStatusCode.NotFound) : 
                new ApiResponse<FundingConfiguration>(System.Net.HttpStatusCode.OK, fundingConfiguration));
        }

        public Task<ApiResponse<FundingPeriod>> GetFundingPeriodById(string fundingPeriodId)
        {
            ApiResponse<FundingPeriod> result;
            if (_fundingPeriods.ContainsKey(fundingPeriodId))
            {
                result = new ApiResponse<FundingPeriod>(System.Net.HttpStatusCode.OK, _fundingPeriods[fundingPeriodId]);
            }
            else
            {
                result = new ApiResponse<FundingPeriod>(System.Net.HttpStatusCode.NotFound);
            }

            return Task.FromResult(result);
        }

        public async Task<ApiResponse<TemplateMetadataContents>> GetFundingTemplateContents(string fundingStreamId, string templateVersion)
        {
            ApiResponse<string> fundingSourceFile = await GetFundingTemplateSourceFile(fundingStreamId, templateVersion);
            if (fundingSourceFile.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new ApiResponse<TemplateMetadataContents>(fundingSourceFile.StatusCode);
            }

            Common.TemplateMetadata.Schema10.TemplateMetadataGenerator templateGenerator = new Common.TemplateMetadata.Schema10.TemplateMetadataGenerator(_logger);
            return await Task.FromResult(new ApiResponse<TemplateMetadataContents>(System.Net.HttpStatusCode.OK, templateGenerator.GetMetadata(fundingSourceFile.Content)));
        }

        public Task<ApiResponse<string>> GetFundingTemplateSourceFile(string fundingStreamId, string templateVersion)
        {
            string fileContents = GetResourceString($"{fundingStreamId}{templateVersion}");
            if (string.IsNullOrWhiteSpace(fileContents))
            {
                return Task.FromResult(new ApiResponse<string>(System.Net.HttpStatusCode.NotFound));
            }
            else
            {
                return Task.FromResult(new ApiResponse<string>(System.Net.HttpStatusCode.OK, fileContents));
            }
        }

        public void SaveFundingPeriod(FundingPeriod fundingPeriod)
        {
            Guard.ArgumentNotNull(fundingPeriod, nameof(fundingPeriod));
            Guard.IsNullOrWhiteSpace(fundingPeriod.Id, nameof(fundingPeriod.Id));

            _fundingPeriods[fundingPeriod.Id] = fundingPeriod;
        }

        public Task<ApiResponse<FundingConfiguration>> SaveFundingConfiguration(string fundingStreamId, string fundingPeriodId, FundingConfiguration configuration)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.ArgumentNotNull(configuration, nameof(configuration));

            string fundingConfigurationKey = $"{fundingStreamId}-{fundingPeriodId}";

            _fundingConfigurations[fundingConfigurationKey] = configuration;

            return Task.FromResult(new ApiResponse<FundingConfiguration>(System.Net.HttpStatusCode.OK, configuration));
        }

        public Task<ApiResponse<string>> SaveFundingTemplate(string templateJson)
        {
            throw new NotImplementedException();
        }

        private static string GetResourceString(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream($"CalculateFunding.Publishing.AcceptanceTests.Resources.{resourceName}.json"))
            {
                byte[] bytes = stream.ReadAllBytes();
                if (bytes == null || bytes.Length == 0)
                {
                    return null;
                }

                return Encoding.UTF8.GetString(bytes);
            }
        }

        public Task<ApiResponse<FundingConfiguration>> SaveFundingConfiguration(string fundingStreamId, string fundingPeriodId, FundingConfigurationUpdateViewModel configuration)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<FundingPeriod>>> GetFundingPeriods()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<FundingPeriod>> SaveFundingPeriods(FundingPeriodsUpdateModel fundingPeriodsModel)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<FundingStream>>> GetFundingStreams()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<FundingStream>> GetFundingStreamById(string fundingStreamId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<FundingStream>> SaveFundingStream(FundingStreamUpdateModel fundingStream)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<string>> GetFundingSchemaByVersion(string schemaVersion)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<string>> SaveFundingSchema(string schema)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<FundingTemplateContents>> GetFundingTemplate(string fundingStreamId, string templateVersion)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<FundingConfiguration>>> GetFundingConfigurationsByFundingStreamId(string fundingStreamId)
        {
            throw new NotImplementedException();
        }
    }
}
