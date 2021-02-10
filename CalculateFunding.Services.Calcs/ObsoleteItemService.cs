using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class ObsoleteItemService : IObsoleteItemService
    {
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ILogger _logger;
        private readonly IValidator<ObsoleteItem> _obsoleteItemValidator;

        public ObsoleteItemService(ICalculationsRepository calculationsRepository,
            ILogger logger,
            IValidator<ObsoleteItem> obsoleteItemValidator)
        {
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(obsoleteItemValidator, nameof(obsoleteItemValidator));

            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _obsoleteItemValidator = obsoleteItemValidator;
        }

        public async Task<IActionResult> CreateObsoleteItem(ObsoleteItem obsoleteItem)
        {
            Guard.ArgumentNotNull(obsoleteItem, nameof(obsoleteItem));

            ValidationResult validationResult = await _obsoleteItemValidator.ValidateAsync(obsoleteItem);
            if (!validationResult.IsValid)
            {
                return new BadRequestObjectResult(validationResult.ToModelStateDictionary());
            }

            HttpStatusCode statusCode = await _calculationsRepository.CreateObsoleteItem(obsoleteItem);

            return statusCode == HttpStatusCode.Created ?
               (IActionResult)new CreatedResult(obsoleteItem.Id, obsoleteItem)
               : new InternalServerErrorResult($"Error occurred while creating obsolete item - {obsoleteItem.Id}");
        }

        public async Task<IActionResult> AddCalculationToObsoleteItem(string obsoleteItemId, string calculationId)
        {
            Guard.ArgumentNotNull(obsoleteItemId, nameof(obsoleteItemId));
            Guard.ArgumentNotNull(calculationId, nameof(calculationId));

            ObsoleteItem obsoleteItem = await _calculationsRepository.GetObsoleteItemById(obsoleteItemId);
            if (obsoleteItem == null)
            {
                string message = $"Obsolete item not found for given obsolete item id - {obsoleteItemId}";
                _logger.Error(message);
                return new NotFoundObjectResult(message);
            }

            Models.Calcs.Calculation calculation = await _calculationsRepository.GetCalculationById(calculationId);
            if (calculation == null)
            {
                string message = $"Calculation not found for given calculation id - {calculationId}";
                _logger.Error(message);
                return new NotFoundObjectResult(message);
            }

            List<string> calculationIds = obsoleteItem.CalculationIds?.ToList() ?? new List<string>();

            if (!calculationIds.Contains(calculationId))
            {
                calculationIds.Add(calculationId);
                obsoleteItem.CalculationIds = calculationIds;

                HttpStatusCode statusCode = await _calculationsRepository.UpdateObsoleteItem(obsoleteItem);

                return statusCode == HttpStatusCode.OK ?
                   (IActionResult)new OkResult()
                   : new InternalServerErrorResult($"Error occurred while updating obsolete item - {obsoleteItem.Id}");
            }

            return new OkResult();
        }

        public async Task<IActionResult> GetObsoleteItemsForCalculation(string calculationId)
        {
            Guard.ArgumentNotNull(calculationId, nameof(calculationId));

            IEnumerable<ObsoleteItem> obsoleteItems = await _calculationsRepository.GetObsoleteItemsForCalculation(calculationId);

            if (obsoleteItems != null && obsoleteItems.Any())
            {
                return new OkObjectResult(obsoleteItems);
            }

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetObsoleteItemsForSpecification(string specificationId)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            IEnumerable<ObsoleteItem> obsoleteItems = await _calculationsRepository.GetObsoleteItemsForSpecification(specificationId);

            if(obsoleteItems != null && obsoleteItems.Any())
            {
                return new OkObjectResult(obsoleteItems);
            }

            return new NotFoundResult();
        }

        public async Task<IActionResult> RemoveObsoleteItem(string obsoleteItemId, string calculationId)
        {
            Guard.ArgumentNotNull(obsoleteItemId, nameof(obsoleteItemId));
            Guard.ArgumentNotNull(calculationId, nameof(calculationId));

            ObsoleteItem obsoleteItem = await _calculationsRepository.GetObsoleteItemById(obsoleteItemId);
            if (obsoleteItem == null)
            {
                string message = $"Obsolete item not found for given obsolete item id - {obsoleteItemId}";
                _logger.Error(message);
                return new NotFoundObjectResult(message);
            }

            List<string> calculationIds = obsoleteItem.CalculationIds?.ToList() ?? new List<string>();
            calculationIds.Remove(calculationId);

            string errorMessage = $"Error occurred while removing calculation - {calculationId} from obsolete item - {obsoleteItem.Id}";

            if (calculationIds.Any())
            {
                obsoleteItem.CalculationIds = calculationIds;

                HttpStatusCode statusCode = await _calculationsRepository.UpdateObsoleteItem(obsoleteItem);

                return statusCode == HttpStatusCode.OK ?
                   (IActionResult)new NoContentResult()
                   : new InternalServerErrorResult(errorMessage);
            }
            else
            {
                HttpStatusCode statusCode = await _calculationsRepository.DeleteObsoleteItem(obsoleteItemId);

                return statusCode == HttpStatusCode.NoContent ?
                  (IActionResult)new NoContentResult()
                  : new InternalServerErrorResult(errorMessage);
            }
        }
    }
}
