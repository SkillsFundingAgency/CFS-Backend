using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Serilog;

namespace CalculateFunding.Services.Results
{
    public class ResultsService : IResultsService
    {
        private readonly ILogger _logger;
        private readonly IResultsRepository _resultsRepository;
        private readonly IMapper _mapper;
        private readonly ISearchRepository<ProviderIndex> _searchRepository;
	    private readonly IMessengerService _messengerService;
	    private readonly ServiceBusSettings _serviceBusSettings;

	    const string ProcessDatasetSubscription = "dataset-events-datasets";

		public ResultsService(ILogger logger,
            IResultsRepository resultsRepository, 
            IMapper mapper, 
            ISearchRepository<ProviderIndex> searchRepository,  IMessengerService messengerService, ServiceBusSettings serviceBusSettings)
        {
            _logger = logger;
	        _resultsRepository = resultsRepository;
            _mapper = mapper;
            _searchRepository = searchRepository;
	        _messengerService = messengerService;
	        _serviceBusSettings = serviceBusSettings;
        }

	    public Task ProcessDataset(Message message)
	    {
		    throw new NotImplementedException();
	    }


		// TODO - refactor to common 
	    IDictionary<string, string> CreateMessageProperties(DatasetMetadataModel metadataModel)
	    {
		    IDictionary<string, string> properties = new Dictionary<string, string>();
		    // TODO - where does correlation ID come from should it be a blob metadata property?  properties.Add("sfa-correlationId", metadataModel???);

			    properties.Add("user-id", metadataModel.AuthorId);
			    properties.Add("user-name", metadataModel.AuthorName);

		    return properties;
	    }

	    public Task UpdateProviderData(Message message)
	    {
		    throw new NotImplementedException();
	    }

	    public async Task<IActionResult> GetProviderResults(HttpRequest request)
	    {
		    var providerId = GetParameter(request, "providerId");
		    var specificationId = GetParameter(request, "specificationId");

			if (string.IsNullOrWhiteSpace(providerId))
		    {
			    _logger.Error("No provider Id was provided to GetProviderResults");
			    return new BadRequestObjectResult("Null or empty provider Id provided");
		    }

		    if (string.IsNullOrWhiteSpace(specificationId))
		    {
			    _logger.Error("No specification Id was provided to GetProviderResults");
			    return new BadRequestObjectResult("Null or empty specification Id provided");
		    }

			ProviderResult providerResult = await _resultsRepository.GetProviderResult(providerId, specificationId);

            if (providerResult != null)
		    {
			    _logger.Information($"A result was found for provider id {providerId}, specification id {specificationId}");

			    return new OkObjectResult(providerResult);
		    }

		    _logger.Information($"A result was not found for provider id {providerId}, specification id {specificationId}");

			return new NotFoundResult();
		}

	    public async Task<IActionResult> GetProviderSpecifications(HttpRequest request)
	    {
		    var providerId = GetParameter(request, "providerId");
		    if (string.IsNullOrWhiteSpace(providerId))
		    {
			    _logger.Error("No provider Id was provided to GetProviderResults");
			    return new BadRequestObjectResult("Null or empty provider Id provided");
		    }

		    List<ProviderResult> providerResults = await _resultsRepository.GetSpecificationResults(providerId);

		    if (providerResults != null)
		    {
			    _logger.Information($"A results was found for provider id {providerId}");

			    var grouped = providerResults.Where(x => x.Specification != null).GroupBy(x => x.Specification.Id).Select(x => x.First().Specification).ToList();

			    return new OkObjectResult(grouped);
		    }

		    _logger.Information($"Results were not found for provider id {providerId}");

		    return new NotFoundResult();

	    }

	    private static string GetParameter(HttpRequest request, string name)
	    {
		    if (request.Query.TryGetValue(name, out var parameter))
		    {
			    return parameter.FirstOrDefault();
		    }
		    return null;
	    }
    }
}
