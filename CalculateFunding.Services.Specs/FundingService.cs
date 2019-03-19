using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CalculateFunding.Services.Specs
{
    public class FundingService : IFundingService
    {
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly ILogger _logger;

        public FundingService(ISpecificationsRepository specificationsRepository, ICacheProvider cacheProvider, ILogger logger)
        {
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _specificationsRepository = specificationsRepository;
            _cacheProvider = cacheProvider;
            _logger = logger;
        }

        public async Task<IActionResult> GetFundingStreams()
        {
            IEnumerable<FundingStream> fundingStreams = await _cacheProvider.GetAsync<FundingStream[]>(CacheKeys.AllFundingStreams);

            if (fundingStreams.IsNullOrEmpty())
            {
                fundingStreams = await _specificationsRepository.GetFundingStreams();

                if (fundingStreams.IsNullOrEmpty())
                {
                    _logger.Error("No funding streams were returned");

                    fundingStreams = new FundingStream[0];
                }

                await _cacheProvider.SetAsync<FundingStream[]>(CacheKeys.AllFundingStreams, fundingStreams.ToArray());
            }

            return new OkObjectResult(fundingStreams);
        }

        public async Task<IActionResult> GetFundingStreamById(string fundingStreamId)
        {
            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to GetFundingStreamById");

                return new BadRequestObjectResult("Null or empty funding stream Id provided");
            }

            FundingStream fundingStream = await _specificationsRepository.GetFundingStreamById(fundingStreamId);

            if (fundingStream == null)
            {
                _logger.Error($"No funding stream was found for funding stream id : {fundingStreamId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(fundingStream);
        }

        public async Task<IActionResult> GetFundingStreamById(HttpRequest request)
        {
            request.Query.TryGetValue("fundingStreamId", out StringValues funStreamId);
            string fundingStreamId = funStreamId.FirstOrDefault();

            return await GetFundingStreamById(fundingStreamId);
        }

        public async Task<IActionResult> SaveFundingStream(HttpRequest request)
        {
            string yaml = await request.GetRawBodyStringAsync();

            string yamlFilename = request.GetYamlFileNameFromRequest();

            if (string.IsNullOrEmpty(yaml))
            {
                _logger.Error($"Null or empty yaml provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            FundingStream fundingStream = null;

            try
            {
                fundingStream = deserializer.Deserialize<FundingStream>(yaml);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Invalid yaml was provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            try
            {
                HttpStatusCode result = await _specificationsRepository.SaveFundingStream(fundingStream);

                if (!result.IsSuccess())
                {
                    int statusCode = (int)result;

                    _logger.Error($"Failed to save yaml file: {yamlFilename} to cosmos db with status {statusCode}");

                    return new StatusCodeResult(statusCode);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Exception occurred writing to yaml file: {yamlFilename} to cosmos db");

                return new StatusCodeResult(500);
            }

            _logger.Information($"Successfully saved file: {yamlFilename} to cosmos db");

            bool keyExists = await _cacheProvider.KeyExists<FundingStream[]>(CacheKeys.AllFundingStreams);

            if (keyExists)
            {
                await _cacheProvider.KeyDeleteAsync<FundingStream[]>(CacheKeys.AllFundingStreams);
            }

            return new OkResult();
        }

        public async Task<IActionResult> GetFundingPeriods(HttpRequest request)
        {
            IEnumerable<Period> fundingPeriods = await _cacheProvider.GetAsync<Period[]>(CacheKeys.FundingPeriods);

            if (fundingPeriods.IsNullOrEmpty())
            {
                fundingPeriods = await _specificationsRepository.GetPeriods();

                if (!fundingPeriods.IsNullOrEmpty())
                {
                    await _cacheProvider.SetAsync<Period[]>(CacheKeys.FundingPeriods, fundingPeriods.ToArraySafe(), TimeSpan.FromDays(100), true);
                }
                else
                {
                    return new InternalServerErrorResult("Failed to find any funding periods");
                }
            }

            return new OkObjectResult(fundingPeriods);
        }

        public async Task<IActionResult> GetFundingPeriodById(HttpRequest request)
        {
            request.Query.TryGetValue("fundingPeriodId", out StringValues fundingPeriodIdParse);

            string fundingPeriodId = fundingPeriodIdParse.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period was provided to GetFundingPeriodById");

                return new BadRequestObjectResult("Null or empty funding period id provided");
            }

            Period fundingPeriod = await _specificationsRepository.GetPeriodById(fundingPeriodId);

            if (fundingPeriod == null)
            {
                _logger.Error($"No funding period was returned for funding period id: {fundingPeriodId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(fundingPeriod);
        }

        public async Task<IActionResult> SaveFundingPeriods(HttpRequest request)
        {
            string yaml = await request.GetRawBodyStringAsync();

            string yamlFilename = request.GetYamlFileNameFromRequest();

            if (string.IsNullOrEmpty(yaml))
            {
                _logger.Error($"Null or empty yaml provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            FundingPeriodsYamlModel fundingPeriodsYamlModel = null;

            try
            {
                fundingPeriodsYamlModel = deserializer.Deserialize<FundingPeriodsYamlModel>(yaml);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Invalid yaml was provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            try
            {
                if (!fundingPeriodsYamlModel.FundingPeriods.IsNullOrEmpty())
                {
                    await _specificationsRepository.SavePeriods(fundingPeriodsYamlModel.FundingPeriods);

                    await _cacheProvider.SetAsync<Period[]>(CacheKeys.FundingPeriods, fundingPeriodsYamlModel.FundingPeriods, TimeSpan.FromDays(100), true);

                    _logger.Information($"Upserted {fundingPeriodsYamlModel.FundingPeriods.Length} funding periods into cosomos");
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Exception occurred writing to yaml file: {yamlFilename} to cosmos db");

                return new StatusCodeResult(500);
            }

            _logger.Information($"Successfully saved file: {yamlFilename} to cosmos db");

            return new OkResult();
        }
    }
}
