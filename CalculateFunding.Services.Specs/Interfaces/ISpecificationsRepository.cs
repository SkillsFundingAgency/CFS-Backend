using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using Microsoft.Azure.Documents;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsRepository
    {
        Task<DocumentEntity<Specification>> CreateSpecification(Specification specification);
        Task<Specification> GetSpecificationById(string specificationId);
        Task<DocumentEntity<Specification>> GetSpecificationDocumentEntityById(string specificationId);
        Task<IEnumerable<Specification>> GetSpecificationsByQuery(Expression<Func<Specification, bool>> query = null);
        Task<IEnumerable<Specification>> GetSpecificationsSelectedForFundingByPeriod(string fundingPeriodId);
        Task<IEnumerable<Specification>> GetSpecifications();
        Task<Specification> GetSpecificationByQuery(Expression<Func<Specification, bool>> query);
        Task<HttpStatusCode> UpdateSpecification(Specification specification);
        Task<Calculation> GetCalculationBySpecificationIdAndCalculationName(string specificationId, string calculationName);
        Task<Calculation> GetCalculationBySpecificationIdAndCalculationId(string specificationId, string calculationId);
        [Obsolete]
        Task<IEnumerable<T>> GetSpecificationsByRawQuery<T>(string sql);
        Task<IEnumerable<T>> GetSpecificationsByRawQuery<T>(SqlQuerySpec sqlQuerySpec);
        Task<IEnumerable<Calculation>> GetCalculationsBySpecificationId(string specificationId);
        Task<IEnumerable<Specification>> GetApprovedOrUpdatedSpecificationsByFundingPeriodAndFundingStream(string fundingPeriodId, string fundingStreamId);
        Task<TemplateMapping> GetTemplateMappingForSpecificationId(string specificationId);
    }
}