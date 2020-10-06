using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CalculateFunding.Api.FundingDataZone.Controllers
{
    [ApiController]
    public class ProviderDataController : ControllerBase
    {
        private readonly IProvidersInSnapshotRetrievalService _providerSnapshotRetrievalService;
        private readonly IProviderRetrievalService _providerRetrievalService;
        private readonly ILocalAuthorityRetrievalService _localAuthorityRetrievalService;
        private readonly IOrganisationsRetrievalService _organisationsRetrievalService;

        public ProviderDataController(
            IProvidersInSnapshotRetrievalService providerSnapshotRetrievalService,
            IProviderRetrievalService providerRetrievalService,
            ILocalAuthorityRetrievalService localAuthorityRetrievalService,
            IOrganisationsRetrievalService organisationsRetrievalService)
        {
            _providerSnapshotRetrievalService = providerSnapshotRetrievalService;
            _providerRetrievalService = providerRetrievalService;
            _localAuthorityRetrievalService = localAuthorityRetrievalService;
            _organisationsRetrievalService = organisationsRetrievalService;
        }

        private const string GetProvidersInSnapshotDescription = @"
```
Given I am CFS user
And I provide a provider snapshot ID
And the provider snapshot exists
When I get all providers for a snapshot
Then I can get a complete list of providers
And I can see all provider properties
And I can see the payment organisation and their properties
```
Used as input for:

- Calc engine as list of scoped providers
- Publishing when refreshing funding
- As in import when this snapshot is used as the current provider list for a given funding stream
";

        /// <summary>
        /// Get all providers in a provider snapshot
        /// </summary>
        /// <param name="providerSnapshotId">Provider Snapshot Id</param>
        /// <returns>All providers in the snapshot with the supplied id</returns>
        [SwaggerOperation(Summary = "Get all providers in a provider snapshot", Description = GetProvidersInSnapshotDescription)]
        [HttpGet("api/providers/snapshots/{providerSnapshotId}/providers")]
        [Produces(typeof(IEnumerable<Provider>))]
        public async Task<ActionResult<IEnumerable<Provider>>> GetProvidersInSnapshot(
            [FromRoute] int providerSnapshotId)
        {
            IEnumerable<Provider> providers = 
                await _providerSnapshotRetrievalService.GetProvidersInSnapshot(providerSnapshotId);

            if (!providers.Any())
            {
                return new NotFoundObjectResult(new ProblemDetails() { Title = "Provider Snapshot not found" });
            }

            return new OkObjectResult(providers);
        }

        private const string GetProviderInSnapshotDescription = @"
```
Given I am CFS user
And I request a provider snapshot ID which exists
And I request a provider by id which exists
When I get details for a single provider within a snapshot
And I am able to get all provider properties
And I am able to get the payment organisation and its properties
```
Used as input for:

- Displaying provider details in calculation results
";
        /// <summary>
        /// Get a single provider within a provider snapshot
        /// </summary>
        /// <param name="providerSnapshotId">Provider Snapshot Id</param>
        /// <param name="providerId">Provider ID</param>
        /// <returns></returns>
        [SwaggerOperation(Summary = "Get a single provider within a provider snapshot", Description = GetProviderInSnapshotDescription)]
        [HttpGet("api/providers/snapshots/{providerSnapshotId}/providers/{providerId}")]
        [Produces(typeof(Provider))]
        public async Task<ActionResult<Provider>> GetProviderInSnapshot(
            [FromRoute] int providerSnapshotId,
            [FromRoute] string providerId)
        {
            Provider provider = await _providerRetrievalService.GetProviderInSnapshot(providerSnapshotId, providerId);

            if (provider == null)
            {
                return new NotFoundObjectResult(new ProblemDetails() { Title = "Provider not found", Status = 404 });
            }

            return new OkObjectResult(provider);
        }

        private const string GetLocalAuthoritiesDescription = @"
```
Given I am a CFS user
When I am viewing providers and wish to filter by one or more local authorities
Then I am see a full list of local authorities to filter by
```

Used in autocomplete dropdown controls
- View funding page to filter by local authority
- Calculation results when filtering by local authority
";

        /// <summary>
        /// Get all local authorities in provider snapshot
        /// </summary>
        /// <param name="providerSnapshotId">Provider Snapshot Id</param>
        /// <returns></returns>
        [SwaggerOperation(Summary = "Get all local authorities in provider snapshot", Description = GetLocalAuthoritiesDescription)]
        [HttpGet("api/providers/snapshots/{providerSnapshotId}/localAuthorities")]
        [Produces(typeof(IEnumerable<PaymentOrganisation>))]
        public async Task<ActionResult<IEnumerable<PaymentOrganisation>>> GetLocalAuthorities(
            [FromRoute] int providerSnapshotId)
        {
            IEnumerable<PaymentOrganisation> localAuthorities = 
                await _localAuthorityRetrievalService.GetLocalAuthorities(providerSnapshotId);

            if (localAuthorities == null || !localAuthorities.Any())
            {
                return new NotFoundObjectResult(new ProblemDetails() { Title = "Provider snapshot not found or does not contain local authorities", Status = 404 });
            }

            return new OkObjectResult(localAuthorities);
        }

        private const string GetAllOrganisationsDescription = @"
```
Given I am a CFS user
When I am viewing providers and wish to filter by one or organisation
Then I am see a full list of organisations to filter by
```

Used in autocomplete dropdown controls
- View funding page to filter by provider organisations (eg MAT)
- Could be used in refresh funding to generate groups for the external API. Eg resolve Organisation name and UKRPN based on trust code/LA Code of the provider
";

        /// <summary>
        /// Get all payment organisations within a provider snapshot
        /// </summary>
        /// <param name="providerSnapshotId">Provider Snapshot Id</param>
        /// <returns></returns>
        [SwaggerOperation(Summary = "Get all payment organisations within a provider snapshot", Description = GetAllOrganisationsDescription)]
        [HttpGet("api/providers/snapshots/{providerSnapshotId}/organisations")]
        [Produces(typeof(IEnumerable<PaymentOrganisation>))]
        public async Task<ActionResult<IEnumerable<PaymentOrganisation>>> GetAllOrganisations(
            [FromRoute] int providerSnapshotId)
        {
            IEnumerable<PaymentOrganisation> organisations = 
                await _organisationsRetrievalService.GetAllOrganisations(providerSnapshotId);

            if (organisations == null || !organisations.Any())
            {
                return new NotFoundObjectResult(new ProblemDetails() { Title = "Provider snapshot not found or does not contain any organisations", Status = 404 });
            }

            return new OkObjectResult(organisations);
        }
    }
}
