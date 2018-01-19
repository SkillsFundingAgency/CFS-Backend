
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationService : ICalculationService
    {
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ILogger _logger;

        public CalculationService(ICalculationsRepository calculationsRepository, ILogger logger)
        {
            _calculationsRepository = calculationsRepository;
            _logger = logger;
        }

        async public Task CreateCalculation(Message message)
        {
            Reference user = message.GetUserDetails();

            Calculation calculation = message.GetPayloadAsInstanceOf<Calculation>();

            if (calculation == null || calculation.Id == null)
            {
                _logger.Error("A null or invalid calculation was provided to CalculateFunding.Services.Calcs.CreateCalculation");

            }
            else
            {
                calculation.Current = new CalculationVersion
                {
                    PublishStatus = PublishStatus.Draft,
                    Author = user,
                    Date = DateTime.UtcNow,
                    Version = 1,
                    DecimalPlaces = 6
                };
               
                HttpStatusCode result = await _calculationsRepository.CreateDraftCalculation(calculation);

                if (result != HttpStatusCode.OK)
                {
                    _logger.Error($"There was problem creating a new calculation with id {calculation.Id} in Cosmos Db with status code {(int)result}");
                }
                else
                {
                    _logger.Information($"Calculation with id: {calculation.Id} was succesfully saved to Cosmos Db");

                    //Add to search
                }
            }
        }
    }
}
