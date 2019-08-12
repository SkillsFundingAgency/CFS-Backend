using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class ApplyTemplateCalculationsService : IApplyTemplateCalculationsService
    {
        private readonly ICalculationService _calculationService;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly Policy _policiesResiliencePolicy;
        private readonly ILogger _logger;

        public ApplyTemplateCalculationsService(ICalculationService calculationService,
            IPoliciesApiClient policiesApiClient,
            ICalcsResiliencePolicies calculationsResiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(calculationService, nameof(calculationService));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.PoliciesApiClient, nameof(calculationsResiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _calculationService = calculationService;
            _policiesApiClient = policiesApiClient;
            _policiesResiliencePolicy = calculationsResiliencePolicies.PoliciesApiClient;
            _logger = logger;
        }
        
        /*
         * -- ignore this Use the TemplateMappingService created in user story #23194 to create and get the mappings for each funding stream of the specification.

Get the contents of the template using the Policy API client from the metadata endpoint (api/templates/{fundingStreamId}/{templateVersion}/metadata). Use the template ID and Funding Stream ID from the message properties of the service bus message.

For any calculations with a null calculation ID, create a new Calculation and hardcode the following values:
Source Code: Return 0
CalculationType: Template
WasTemplateCalculation: false
Description: null

Lookup the calculation in the template contents (match the TemplateCalculationId from the contents and the mapping) and set the following properties from the template:
Name
ValueType

Ensure all calculations have Draft status on creation.

Once all of the calculations are saved for the spec, update the TemplateMapping for this specification with the corresponding CalculationIds.

Create a job update notification for every 10 calculations which have been created to update the progress of creating calculations for the overall job.

Initiate a calculation run after completing.
         * 
         */

        public async Task ApplyTemplateCalculation(Message message)
        {
            //TODO: implement this for 22046
            
            //step 1  public async Task<IActionResult> AssociateTemplateIdWithSpecification(string specificationId, string templateVersion, string fundingStreamId)

            await Task.CompletedTask;
        }
    }
}