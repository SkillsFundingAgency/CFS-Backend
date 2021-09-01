using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing.Interfaces;
using System;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class CurrentSpecificationStepContext : ICurrentSpecificationStepContext
    {
        private readonly ISpecificationService _repo;
        private readonly ISpecificationsApiClient _apiClient;

        public CurrentSpecificationStepContext(ISpecificationService repo,
            ISpecificationsApiClient apiClient)
        {
            _repo = repo;
            _apiClient = apiClient;
        }

        public string SpecificationId { get; set; }

        public SpecificationInMemoryRepository Repo => (SpecificationInMemoryRepository)_repo;

        public SpecificationsInMemoryClient ApiClient => (SpecificationsInMemoryClient)_apiClient;

        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    }
}
