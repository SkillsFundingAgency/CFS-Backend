using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryPublishedFundingRepository : IPublishedFundingRepository
    {
        Dictionary<string, List<PublishedProvider>> _publishedProviders = new Dictionary<string, List<PublishedProvider>>();

        // Keyed on SpecificationId
        Dictionary<string, List<PublishedFunding>> _publishedFunding = new Dictionary<string, List<PublishedFunding>>();

        public Task<IEnumerable<PublishedFunding>> GetLatestPublishedFundingBySpecification(string specificationId)
        {
            IEnumerable<PublishedFunding> result = null;

            if (_publishedFunding.ContainsKey(specificationId))
            {
                result = _publishedFunding[specificationId];
            }

            return Task.FromResult(result);
        }

        public Task<IEnumerable<PublishedProvider>> GetLatestPublishedProvidersBySpecification(string specificationId)
        {
            IEnumerable<PublishedProvider> result = null;
            if (_publishedProviders.ContainsKey(specificationId))
            {
                result = _publishedProviders[specificationId];
            }

            return Task.FromResult(result);
        }

        public Task<IEnumerable<PublishedProvider>> GetPublishedProvidersForApproval(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<PublishedProviderVersion> GetPublishedProviderVersion(string fundingStreamId, string fundingPeriodId, string providerId, string version)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> UpsertPublishedFunding(PublishedFunding publishedFunding)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(publishedFunding.Current, nameof(publishedFunding.Current));
            Guard.IsNullOrWhiteSpace(publishedFunding.Current.SpecificationId, nameof(publishedFunding.Current.SpecificationId));
            Guard.IsNullOrWhiteSpace(publishedFunding.Id, nameof(publishedFunding.Id));


            string specificationId = publishedFunding.Current.SpecificationId;

            if (!_publishedFunding.ContainsKey(specificationId))
            {
                _publishedFunding.Add(specificationId, new List<PublishedFunding>());
            }

            PublishedFunding existingFunding = _publishedFunding[specificationId].Where(p => p.Id == publishedFunding.Id).FirstOrDefault();

            if (existingFunding != null)
            {
                _publishedFunding[specificationId].Remove(existingFunding);
            }

            _publishedFunding[specificationId].Add(publishedFunding);

            return Task.FromResult(HttpStatusCode.OK);
        }

        public Task<IEnumerable<HttpStatusCode>> UpsertPublishedProviders(IEnumerable<PublishedProvider> publishedProviders)
        {
            throw new NotImplementedException();
        }

        public Task<PublishedProvider> AddPublishedProvider(string specificationId, PublishedProvider publishedProvider)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(publishedProvider, nameof(publishedProvider));

            if (!_publishedProviders.ContainsKey(specificationId))
            {
                _publishedProviders[specificationId] = new List<PublishedProvider>();
            }

            _publishedProviders[specificationId].Add(publishedProvider);

            return Task.FromResult(publishedProvider);
        }
    }
}
