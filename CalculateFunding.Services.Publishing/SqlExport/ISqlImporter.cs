using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface ISqlImporter
    {
        Task ImportData(string specificationId,
            string fundingStreamId,
            SchemaContext schemaContext);
    }
}