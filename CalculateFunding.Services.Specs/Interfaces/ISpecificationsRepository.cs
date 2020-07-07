using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsRepository
    {
        Task<DocumentEntity<Specification>> CreateSpecification(Specification specification);

        Task<Specification> GetSpecificationById(string specificationId);

        Task<DocumentEntity<Specification>> GetSpecificationDocumentEntityById(string specificationId);

        Task<IEnumerable<Specification>> GetSpecificationsByQuery(Expression<Func<DocumentEntity<Specification>, bool>> query = null);

        Task<IEnumerable<Specification>> GetSpecificationsSelectedForFundingByPeriod(string fundingPeriodId);

        Task<IEnumerable<Specification>> GetSpecificationsSelectedForFundingByPeriodAndFundingStream(
            string fundingPeriodId, string fundingStreamId);

        Task<IEnumerable<Specification>> GetSpecifications();

        Task<Specification> GetSpecificationByQuery(Expression<Func<DocumentEntity<Specification>, bool>> query);

        Task<HttpStatusCode> UpdateSpecification(Specification specification);

        Task<IEnumerable<T>> GetSpecificationsByRawQuery<T>(CosmosDbQuery cosmosDbQuery);

        Task<IEnumerable<Specification>> GetApprovedOrUpdatedSpecificationsByFundingPeriodAndFundingStream(
            string fundingPeriodId, string fundingStreamId);

        Task<IEnumerable<Specification>> GetSpecificationsByFundingPeriodAndFundingStream(
            string fundingPeriodId, string fundingStreamId);

        Task DeleteSpecifications(string specificationId, DeletionType deletionType);
        Task<IEnumerable<string>> GetDistinctFundingStreamsForSpecifications();
    }
}