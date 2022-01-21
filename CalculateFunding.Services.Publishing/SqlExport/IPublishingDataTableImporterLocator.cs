using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface IPublishingDataTableImporterLocator
    {
        IPublishingDataTableImporter GetService(SqlExportSource sqlExportSource);
    }
}
