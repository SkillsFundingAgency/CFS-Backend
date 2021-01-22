using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface IQaSchemaService
    {
        Task<SchemaContext> EnsureSqlTablesForSpecification(string specificationId);

        Task<SchemaContext> ReCreateTablesForSpecificationAndFundingStream(string specificationId,
            string fundingStreamId);
    }
}
