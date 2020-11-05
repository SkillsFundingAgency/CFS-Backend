using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models.Versioning;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class SpecificationInMemoryRepository : ISpecificationService
    {
        private readonly Dictionary<string, SpecificationSummary> _specifications = new Dictionary<string, SpecificationSummary>();
        private readonly Dictionary<string, IEnumerable<ProfileVariationPointer>> _variationPointers = new Dictionary<string, IEnumerable<ProfileVariationPointer>>();

        public Task<IEnumerable<SpecificationSummary>> GetSpecificationsSelectedForFundingByPeriod(string fundingPeriodId)
        {
            IEnumerable<SpecificationSummary> specifications = _specifications.Values
                 .Where(c => c.IsSelectedForFunding && c.FundingPeriod.Id == fundingPeriodId);

            return Task.FromResult(specifications);
        }

        public Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId)
        {
            _specifications.TryGetValue(specificationId, value: out SpecificationSummary result);

            return Task.FromResult(result);
        }

        public Task SelectSpecificationForFunding(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<SpecificationSummary> AddSpecification(SpecificationSummary specificationSummary)
        {
            _specifications.Add(specificationSummary.Id, specificationSummary);
            return Task.FromResult(specificationSummary);
        }

        public void AddVariationPointers(string specificationId,
            params ProfileVariationPointer[] variationPointers)
        {
            _variationPointers[specificationId] = variationPointers;
        }

        public Task<IEnumerable<ProfileVariationPointer>> GetProfileVariationPointers(string specificationId)
        {
            return _variationPointers.TryGetValue(specificationId, out IEnumerable<ProfileVariationPointer> variationPointers) ? Task.FromResult(variationPointers) : Task.FromResult((IEnumerable<ProfileVariationPointer>)null);
        }

        public Task<PublishStatusResponseModel> EditSpecificationStatus(string specificationId, PublishStatus publishStatus)
        {
            if (_specifications.TryGetValue(specificationId, out SpecificationSummary result))
            {
                result.ApprovalStatus = (Common.ApiClient.Models.PublishStatus) 
                    Enum.Parse(typeof(Common.ApiClient.Models.PublishStatus), publishStatus.ToString());
            }

            return Task.FromResult(new PublishStatusResponseModel { PublishStatus = publishStatus });
        }
    }
}
