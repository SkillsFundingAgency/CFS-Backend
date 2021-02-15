using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs.Analysis.ObsoleteItems
{
    public class ObsoleteItemService : IObsoleteItemService
    {
        private readonly AsyncPolicy _calculationsResilience;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IUniqueIdentifierProvider _uniqueIdentifierProvider;
        private readonly ILogger _logger;
        private readonly IValidator<ObsoleteItem> _obsoleteItemValidator;

        public ObsoleteItemService(ICalculationsRepository calculationsRepository,
            ILogger logger,
            IValidator<ObsoleteItem> obsoleteItemValidator,
            ICalcsResiliencePolicies resiliencePolicies,
            IUniqueIdentifierProvider uniqueIdentifierProvider)
        {
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(obsoleteItemValidator, nameof(obsoleteItemValidator));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(uniqueIdentifierProvider, nameof(uniqueIdentifierProvider));

            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _obsoleteItemValidator = obsoleteItemValidator;
            _uniqueIdentifierProvider = uniqueIdentifierProvider;
            _calculationsResilience = resiliencePolicies.CalculationsRepository;
        }

        public async Task<IActionResult> CreateObsoleteItem(ObsoleteItem obsoleteItem)
        {
            Guard.ArgumentNotNull(obsoleteItem, nameof(obsoleteItem));

            ValidationResult validationResult = await _obsoleteItemValidator.ValidateAsync(obsoleteItem);
            
            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            obsoleteItem.Id = _uniqueIdentifierProvider.CreateUniqueIdentifier();

            HttpStatusCode statusCode = await _calculationsResilience.ExecuteAsync(() 
                => _calculationsRepository.CreateObsoleteItem(obsoleteItem));

            return statusCode == HttpStatusCode.Created ?
               (IActionResult)new CreatedResult(obsoleteItem.Id, obsoleteItem)
               : new InternalServerErrorResult($"Error occurred while creating obsolete item - {obsoleteItem.Id}");
        }

        public async Task<IActionResult> AddCalculationToObsoleteItem(string obsoleteItemId, string calculationId)
        {
            Guard.ArgumentNotNull(obsoleteItemId, nameof(obsoleteItemId));
            Guard.ArgumentNotNull(calculationId, nameof(calculationId));

            ObsoleteItem obsoleteItem = await _calculationsResilience.ExecuteAsync(() 
                => _calculationsRepository.GetObsoleteItemById(obsoleteItemId));
            
            if (obsoleteItem == null)
            {
                string message = $"Obsolete item not found for given obsolete item id - {obsoleteItemId}";
                _logger.Error(message);
                return new NotFoundObjectResult(message);
            }

            Calculation calculation = await _calculationsResilience.ExecuteAsync(() 
                => _calculationsRepository.GetCalculationById(calculationId));
            
            if (calculation == null)
            {
                string message = $"Calculation not found for given calculation id - {calculationId}";
                _logger.Error(message);
                return new NotFoundObjectResult(message);
            }

            if (obsoleteItem.TryAddCalculationId(calculationId))
            {
                HttpStatusCode statusCode = await _calculationsResilience.ExecuteAsync(() 
                    => _calculationsRepository.UpdateObsoleteItem(obsoleteItem));

                return statusCode == HttpStatusCode.OK ?
                   (IActionResult)new OkResult()
                   : new InternalServerErrorResult($"Error occurred while updating obsolete item - {obsoleteItem.Id}");
            }

            return new OkResult();
        }

        public async Task<IActionResult> GetObsoleteItemsForCalculation(string calculationId)
        {
            Guard.ArgumentNotNull(calculationId, nameof(calculationId));

            IEnumerable<ObsoleteItem> obsoleteItems = await _calculationsResilience.ExecuteAsync(() 
                => _calculationsRepository.GetObsoleteItemsForCalculation(calculationId));

            if (obsoleteItems != null && obsoleteItems.Any())
            {
                return new OkObjectResult(obsoleteItems);
            }

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetObsoleteItemsForSpecification(string specificationId)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            IEnumerable<ObsoleteItem> obsoleteItems = await _calculationsResilience.ExecuteAsync(() 
                => _calculationsRepository.GetObsoleteItemsForSpecification(specificationId));

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

            ObsoleteItem obsoleteItem = await _calculationsResilience.ExecuteAsync(() 
                => _calculationsRepository.GetObsoleteItemById(obsoleteItemId));
            
            if (obsoleteItem == null)
            {
                string message = $"Obsolete item not found for given obsolete item id - {obsoleteItemId}";
                _logger.Error(message);
                return new NotFoundObjectResult(message);
            }
            
            obsoleteItem.TryRemoveCalculationId(calculationId);

            string errorMessage = $"Error occurred while removing calculation - {calculationId} from obsolete item - {obsoleteItem.Id}";

            if (!obsoleteItem.IsEmpty)
            {
                HttpStatusCode statusCode = await _calculationsResilience.ExecuteAsync(() 
                    => _calculationsRepository.UpdateObsoleteItem(obsoleteItem));

                return statusCode == HttpStatusCode.OK ?
                   (IActionResult)new NoContentResult()
                   : new InternalServerErrorResult(errorMessage);
            }
            else
            {
                HttpStatusCode statusCode = await _calculationsResilience.ExecuteAsync(() 
                    => _calculationsRepository.DeleteObsoleteItem(obsoleteItemId));

                return statusCode == HttpStatusCode.NoContent ?
                  (IActionResult)new NoContentResult()
                  : new InternalServerErrorResult(errorMessage);
            }
        }
    }
}
