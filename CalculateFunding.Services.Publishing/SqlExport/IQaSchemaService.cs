using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface IQaSchemaService
    {
        Task EnsureSqlTablesForSpecification(string specificationId);

        Task ReCreateTablesForSpecificationAndFundingStream(string specificationId,
            string fundingStreamId);
    }
}
