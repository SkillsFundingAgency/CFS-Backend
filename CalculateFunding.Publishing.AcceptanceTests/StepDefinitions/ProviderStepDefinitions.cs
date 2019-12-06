using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class ProviderStepDefinitions
    {
        private readonly IProvidersStepContext _providersStepContext;
        private readonly ICurrentSpecificationStepContext _currentSpecificationStepContext;

        public ProviderStepDefinitions(
            IProvidersStepContext providersStepContext,
            ICurrentSpecificationStepContext currentSpecificationStepContext)
        {
            _providersStepContext = providersStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
        }

        [Given(@"the following provider version exists in the providers service")]
        public void GivenTheFollowingProviderVersionExistsInTheProvidersService(Table table)
        {
            ProviderVersion providerVersion = table.CreateInstance<ProviderVersion>();

            _providersStepContext.EmulatedClient.AddProviderVersion(providerVersion);
        }


        [Given(@"the following provider exists within core provider data in provider version '(.*)'")]
        public void GivenTheFollowingProviderExistsWithinCoreProviderDataInProviderVersion(string providerVersionId, Table table)
        {
            providerVersionId
                .Should()
                .NotBeNull();

            Provider provider = table.CreateInstance<Provider>();

            provider.Should().NotBeNull("Provider should not be null");

            provider.ProviderId.Should().NotBeNull("ProviderId should not be null");

            _providersStepContext.EmulatedClient.AddProviderToCoreProviderData(providerVersionId, provider);
        }

        [Given(@"the provider with id '(.*)' should be a scoped provider in the current specification in provider version '(.*)'")]
        public void GivenTheProviderWithIdShouldBeAScopedProviderInTheCurrentSpecificationInProviderVersion(string providerId, string providerVersionId)
        {
            providerId
                .Should()
                .NotBeNullOrWhiteSpace("provider ID should not be null or empty string");

            providerVersionId
                .Should()
                .NotBeNullOrWhiteSpace("provider version ID should not be null or empty string");

            _currentSpecificationStepContext.SpecificationId
                .Should()
                .NotBeNullOrWhiteSpace("current specification ID should not be null or empty string");

            _providersStepContext.EmulatedClient.AddProviderAsScopedProvider(
                _currentSpecificationStepContext.SpecificationId,
                providerVersionId,
                providerId);
        }


    }
}
