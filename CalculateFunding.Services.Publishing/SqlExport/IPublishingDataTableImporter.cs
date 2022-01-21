using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface IPublishingDataTableImporter : IDataTableImporter
    {
        SqlExportSource SqlExportSource { get; }
    }
}
