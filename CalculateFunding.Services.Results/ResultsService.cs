using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
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
    }
}
