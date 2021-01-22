using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface ISqlImportContextBuilder
    {
        Task<ISqlImportContext> CreateImportContext(string specificationId,
            string fundingStreamId,
            SchemaContext schemaContext);
    }
}