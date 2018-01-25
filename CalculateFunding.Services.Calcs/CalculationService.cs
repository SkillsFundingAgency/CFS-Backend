
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationService : ICalculationService
    {
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ILogger _logger;
        private readonly ISearchRepository<CalculationIndex> _searchRepository;
        private readonly IValidator<Calculation> _calculationValidator;
        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private string[] Facets = new string[] { "allocationLineName", "policySpecificationNames", "status", "fundingStreamName" };

        private List<string> Select = new List<string> { "id", "name", "specificationName", "periodName", "status" };

        private IEnumerable<string> DefaultOrderBy = new[] { "lastUpdatedDate desc" };

        public CalculationService(ICalculationsRepository calculationsRepository, ILogger logger,
            ISearchRepository<CalculationIndex> searchRepository, IValidator<Calculation> calculationValidator,
            IBuildProjectsRepository buildProjectsRepository)
        {
            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _searchRepository = searchRepository;
            _calculationValidator = calculationValidator;
            _buildProjectsRepository = buildProjectsRepository;
        }

        async public Task<IActionResult> SearchCalculations(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SearchModel searchModel = JsonConvert.DeserializeObject<SearchModel>(json);

            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Warning("A null or invalid search model was provide for searching calculations");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            SearchParameters searchParameters = new SearchParameters
            {
                Skip = (searchModel.PageNumber - 1) * searchModel.Top,
                Top = searchModel.Top,
                Facets = Facets,
                Select = Select,
                SearchMode = SearchMode.Any,
                IncludeTotalResultCount = true,
                OrderBy = searchModel.OrderBy.IsNullOrEmpty() ? DefaultOrderBy.ToList() : searchModel.OrderBy.ToList()
            };

            try
            {
                SearchResults<CalculationIndex> searchResults = await _searchRepository.Search(searchModel.SearchTerm, searchParameters);

                CalculationSearchResults results = new CalculationSearchResults
                {
                    TotalCount = (int)(searchResults?.TotalCount ?? 0),
                    Results = searchResults?.Results.Select(m => new CalculationSearchResult
                    {
                        Id = m.Result.Id,
                        Name = m.Result.Name,
                        PeriodName = m.Result.PeriodName,
                        SpecificationName = m.Result.SpecificationName,
                        Status = m.Result.Status
                    }),
                    Facets = searchResults?.Facets
                };


                return new OkObjectResult(results);
            }
            catch(FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new StatusCodeResult(500);
            }
        }

        async public Task<IActionResult> GetCalculationHistory(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            var calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationHistory");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            IEnumerable<CalculationVersion> history = await _calculationsRepository.GetVersionHistory(calculationId);

            if (history == null)
            {
                _logger.Information($"A calculation was not found for calculation id {calculationId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(history);
        }

        async public Task<IActionResult> GetCalculationVersions(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CalculationVersionsCompareModel compareModel = JsonConvert.DeserializeObject<CalculationVersionsCompareModel>(json);

            //Need custom validator here

            if (compareModel == null || string.IsNullOrEmpty(compareModel.CalculationId) || compareModel.Versions == null || compareModel.Versions.Count() < 2)
            {
                _logger.Warning("A null or invalid compare model was provided for comparing models");

                return new BadRequestObjectResult("A null or invalid compare model was provided for comparing models");
            }

            IEnumerable<CalculationVersion> versions = await _calculationsRepository.GetCalculationVersions(compareModel);

            if (versions == null)
            {
                _logger.Information($"A calculation was not found for calculation id {compareModel.CalculationId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(versions);
        }

        async public Task<IActionResult> GetCalculationById(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            var calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationById");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            Calculation calculation = await _calculationsRepository.GetCalculationById(calculationId);

            if(calculation != null)
            {
                _logger.Information($"A calculation was found for calculation id {calculationId}");

                return new OkObjectResult(calculation);
            }

            _logger.Information($"A calculation was not found for calculation id {calculationId}");

            return new NotFoundResult();
        }

        async public Task CreateCalculation(Message message)
        {
            Reference user = message.GetUserDetails();

            Calculation calculation = message.GetPayloadAsInstanceOf<Calculation>();

            if (calculation == null)
            {
                _logger.Error("A null calculation was provided to CalculateFunding.Services.Calcs.CreateCalculation");
            }
            else
            {
                var validationResult = await _calculationValidator.ValidateAsync(calculation);

                if (!validationResult.IsValid)
                {
                    throw new InvalidModelException(GetType().ToString(), validationResult.Errors.Select(m => m.ErrorMessage).ToArraySafe());
                }

                calculation.Current = new CalculationVersion
                {
                    PublishStatus = PublishStatus.Draft,
                    Author = user,
                    Date = DateTime.UtcNow,
                    Version = 1,
                    DecimalPlaces = 6
                };

                calculation.History = new List<CalculationVersion>
                {
                    new CalculationVersion
                    {
                        PublishStatus = PublishStatus.Draft,
                        Author = user,
                        Date = DateTime.UtcNow,
                        Version = 1,
                        DecimalPlaces = 6
                    }
                };

                calculation.BuildProjectId = Guid.NewGuid().ToString();
               
                HttpStatusCode result = await _calculationsRepository.CreateDraftCalculation(calculation);

                if (result == HttpStatusCode.Created)
                {
                    _logger.Information($"Calculation with id: {calculation.Id} was succesfully saved to Cosmos Db");

                    IList<IndexError> indexingResults = await _searchRepository.Index(new List<CalculationIndex>
                    {
                        new CalculationIndex
                        {
                            Id = calculation.Id,
                            Name = calculation.Name,
                            CalculationSpecificationId = calculation.CalculationSpecification.Id,
                            CalculationSpecificationName = calculation.CalculationSpecification.Name,
                            SpecificationName = calculation.Specification.Name,
                            SpecificationId = calculation.Specification.Id,
                            PeriodId = calculation.Period.Id,
                            PeriodName = calculation.Period.Name,
                            AllocationLineId = calculation.AllocationLine.Id,
                            AllocationLineName = calculation.AllocationLine.Name,
                            PolicySpecificationIds   = calculation.Policies.Select(m => m.Id).ToArraySafe(),
                            PolicySpecificationNames = calculation.Policies.Select(m => m.Name).ToArraySafe(),
                            SourceCode = calculation.Current.SourceCode,
                            Status = calculation.Current.PublishStatus.ToString(),
                            FundingStreamId = calculation.FundingStream.Id,
                            FundingStreamName = calculation.FundingStream.Name,
                            LastUpdatedDate = DateTimeOffset.Now,
                        }
                    });

                    await CreateBuildProject(calculation);
                }
                else
                {
                    _logger.Error($"There was problem creating a new calculation with id {calculation.Id} in Cosmos Db with status code {(int)result}");
                }
            }
        }

        async Task CreateBuildProject(Calculation calculation)
        {
            BuildProject buildproject = new BuildProject
            {
                Calculations = new List<Calculation>
                {
                    calculation
                },
                Specification = calculation.Specification,
                Id = calculation.BuildProjectId,
                Name = calculation.Specification.Name
            };

            await _buildProjectsRepository.CreateBuildProject(buildproject);
        }
    }
}
