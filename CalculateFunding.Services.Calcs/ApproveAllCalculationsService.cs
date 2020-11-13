using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Services.Calcs
{
    public class ApproveAllCalculationsService : JobProcessingService, IApproveAllCalculationsService
    {
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ILogger _logger;
        private readonly Polly.AsyncPolicy _calculationRepositoryPolicy;

        public ApproveAllCalculationsService(
            ICalculationsRepository calculationsRepository,
            ICalcsResiliencePolicies resiliencePolicies,
            ILogger logger,
            IJobManagement jobManagement) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _calculationRepositoryPolicy = resiliencePolicies.CalculationsRepository;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = UserPropertyFrom(message, "specification-id");

            IEnumerable<Calculation> calculations =
                (await _calculationRepositoryPolicy.ExecuteAsync(() =>
                    _calculationsRepository.GetCalculationsBySpecificationId(specificationId)))
                .ToArraySafe();

            if (calculations.IsNullOrEmpty())
            {
                string calcNotFoundMessage = $"No calculations found for specification id: {specificationId}";

                _logger.Information(calcNotFoundMessage);

                return;
            }

            foreach (Calculation calculation in calculations)
            {
                calculation.Current.PublishStatus = Models.Versioning.PublishStatus.Approved;
            }

            await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.UpdateCalculations(calculations));
        }

        private string UserPropertyFrom(Message message, string key)
        {
            string userProperty = message.GetUserProperty<string>(key);

            Guard.IsNullOrWhiteSpace(userProperty, key);

            return userProperty;
        }
    }
}
