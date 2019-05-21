using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Providers.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Serilog;


namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionService : IProviderVersionService
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly IBlobClient _blobClient;
        private readonly ILogger _logger;
        private readonly IValidator<ProviderVersionViewModel> _providerVersionModelValidator;

        public ProviderVersionService(ICacheProvider cacheProvider, IBlobClient blobClient, ILogger logger, IValidator<ProviderVersionViewModel>  providerVersionModelValidator)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(providerVersionModelValidator, nameof(providerVersionModelValidator));

            _cacheProvider = cacheProvider;
            _blobClient = blobClient;
            _logger = logger;
            _providerVersionModelValidator = providerVersionModelValidator;
        }

        public async Task<IActionResult> GetAllProviders(string providerVersionId)
        {
            throw new NotImplementedException();
        }
        public async Task<IActionResult> GetAllProviders(int year, int month, int day)
        {
            throw new NotImplementedException();
        }

        public async Task<IActionResult> UploadProviderVersion(string actionName, string controller, string providerVersionId , ProviderVersionViewModel providers)
        {
            providers.Id = providerVersionId;

            BadRequestObjectResult validationResult = (await _providerVersionModelValidator.ValidateAsync(providers)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            ICloudBlob blob = _blobClient.GetBlockBlobReference(providerVersionId + ".json");

            // convert string to stream
            byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(providers));

            //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
            MemoryStream stream = new MemoryStream(byteArray);

            await blob.UploadFromStreamAsync(stream);

            return new CreatedAtActionResult(actionName, controller, new { providerVersionId = providerVersionId }, providerVersionId);
        }
    }
}
