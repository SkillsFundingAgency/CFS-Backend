
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using FluentValidation;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationService : ICalculationService
    {
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ILogger _logger;
        private readonly ISearchRepository<CalculationIndex> _searchRepository;
        private readonly IValidator<Calculation> _calculationValidator;

        public CalculationService(ICalculationsRepository calculationsRepository, ILogger logger,
            ISearchRepository<CalculationIndex> searchRepository, IValidator<Calculation> calculationValidator)
        {
            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _searchRepository = searchRepository;
            _calculationValidator = calculationValidator;
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
                            FundingStreamName = calculation.FundingStream.Name
                        }
                    });
                }
                else
                {
                    _logger.Error($"There was problem creating a new calculation with id {calculation.Id} in Cosmos Db with status code {(int)result}");
                }
            }
        }
    }
}
