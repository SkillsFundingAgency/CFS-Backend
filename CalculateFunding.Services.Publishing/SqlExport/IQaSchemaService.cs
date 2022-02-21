using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface IQaSchemaService
    {
        Task<SchemaContext> EnsureSqlTablesForSpecification(
            string specificationId,
            SqlExportSource sqlExportSource);

        Task<SchemaContext> ReCreateTablesForSpecificationAndFundingStream(
            string specificationId,
            string fundingStreamId,
            string jobId,
            SqlExportSource sqlExportSource);
    }
}
