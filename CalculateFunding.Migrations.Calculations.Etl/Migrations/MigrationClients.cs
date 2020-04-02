using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Utility;
using Polly;

namespace CalculateFunding.Migrations.Calculations.Etl.Migrations
{
    public class MigrationClients
    {
        private readonly AsyncPolicy _calculationsPolicy;
        private readonly AsyncPolicy _specificationsPolicy;
        private readonly AsyncPolicy _dataSetsPolicy;
        private readonly ICalculationsApiClient _calculations;
        private readonly ISpecificationsApiClient _specifications;
        private readonly IDatasetsApiClient _dataSets;

        public MigrationClients(IServiceProvider services)
        {
            ServiceProviderWrapper serviceProviderWrapper = new ServiceProviderWrapper(services);

            ICalculationsEtlResiliencePolicies policies = serviceProviderWrapper.GetService<ICalculationsEtlResiliencePolicies>();
            
            Guard.ArgumentNotNull(policies?.CalculationsApiClient, nameof(policies.CalculationsApiClient));
            Guard.ArgumentNotNull(policies?.SpecificationApiClient, nameof(policies.SpecificationApiClient));
            Guard.ArgumentNotNull(policies?.DataSetsApiClient, nameof(policies.DataSetsApiClient));

            _calculationsPolicy = policies.CalculationsApiClient;
            _specificationsPolicy = policies.SpecificationApiClient;
            _dataSetsPolicy = policies.DataSetsApiClient;

            ICalculationsApiClient calculations = serviceProviderWrapper.GetService<ICalculationsApiClient>();
            ISpecificationsApiClient specifications = serviceProviderWrapper.GetService<ISpecificationsApiClient>();
            IDatasetsApiClient dataSets = serviceProviderWrapper.GetService<IDatasetsApiClient>();
            
            Guard.ArgumentNotNull(calculations, nameof(ICalculationsApiClient));
            Guard.ArgumentNotNull(specifications, nameof(ISpecificationsApiClient));
            Guard.ArgumentNotNull(dataSets, nameof(IDatasetsApiClient));
            
            _calculations = calculations;
            _specifications = specifications;
            _dataSets = dataSets;
        }

        private class ServiceProviderWrapper
        {
            private readonly IServiceProvider _services;

            public ServiceProviderWrapper(IServiceProvider services)
            {
                _services = services;
            }

            public TService GetService<TService>()
            {
                return (TService) _services.GetService(typeof(TService));
            }
        }

        public Task<TResponse> MakeSpecificationsCall<TResponse>(Func<ISpecificationsApiClient, Task<TResponse>> call)
        {
            return _specificationsPolicy.ExecuteAsync(() => call(_specifications));
        }
        
        public Task<TResponse> MakeDataSetsCall<TResponse>(Func<IDatasetsApiClient, Task<TResponse>> call)
        {
            return _dataSetsPolicy.ExecuteAsync(() => call(_dataSets));
        }
        
        public Task<TResponse> MakeCalculationsCall<TResponse>(Func<ICalculationsApiClient, Task<TResponse>> call)
        {
            return _calculationsPolicy.ExecuteAsync(() => call(_calculations));
        }
    }
}